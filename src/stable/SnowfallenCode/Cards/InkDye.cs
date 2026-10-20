using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;
namespace SnowfallenMod.SnowfallenCode.Cards;

public class InkDye() : SnowfallenCard(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    internal bool AttacksAll => IsMutable && Owner is not null &&
        (BloodThresholdMechanics.IsNormal(Owner.Creature) || BloodThresholdMechanics.IsLow(Owner.Creature));
    public override TargetType TargetType => AttacksAll ? TargetType.AllEnemies : TargetType.AnyEnemy;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<DexterityPower>(), BloodThresholdHoverTip()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(14m, ValueProp.Move), new PowerVar<WeakPower>(2m), new PowerVar<DexterityPower>(1m)];
    internal int BloodDiscount => !IsMutable || Owner is null ? 0 :
        BloodThresholdMechanics.IsLow(Owner.Creature) ? 2 :
        BloodThresholdMechanics.IsNormal(Owner.Creature) ? 1 : 0;
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (!ReferenceEquals(card, this) || card.Pile?.Type is not (PileType.Hand or PileType.Play)) return false;
        modifiedCost = Math.Max(0m, originalCost - BloodDiscount);
        return modifiedCost != originalCost;
    }
    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner.Creature) InvokeEnergyCostChanged();
        return Task.CompletedTask;
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        // Freeze this resolution's mode and target order before damage can change HP.
        bool all = AttacksAll;
        bool low = BloodThresholdMechanics.IsLow(Owner.Creature);
        if (!all) ArgumentNullException.ThrowIfNull(play.Target);
        var targets = all ? CombatState.HittableEnemies.ToList() : new List<Creature> { play.Target };
        var attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3");
        if (all) attack.TargetingAllOpponents(CombatState);
        else attack.Targeting(play.Target);
        await attack.Execute(context);
        foreach (var target in targets.Where(target => !target.IsDead))
            await PowerCmd.Apply<WeakPower>(context, target, DynamicVars.Weak.BaseValue, Owner.Creature, this);
        if (low)
            await PowerCmd.Apply<DexterityPower>(context, Owner.Creature, DynamicVars.Dexterity.BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6m);
}
