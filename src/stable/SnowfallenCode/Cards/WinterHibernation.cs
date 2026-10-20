using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;
namespace SnowfallenMod.SnowfallenCode.Cards;

public class WinterHibernation() : SnowfallenCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(4m, ValueProp.Move), new DynamicVar("HpPerDamage", 3m)];
    internal decimal LossBonus(int hpLost) => Math.Floor(hpLost / DynamicVars["HpPerDamage"].BaseValue);
    public override decimal ModifyDamageAdditive(Creature target, decimal amount,
        ValueProp props, Creature dealer, CardModel cardSource)
    {
        // Corrupted HP loss has this same source and dealer, but is Unpowered.
        // Only this card's own powered attack may receive its growth bonus;
        // retain null-target previews while excluding friendly targets.
        if (!IsMutable || !ReferenceEquals(cardSource, this) || dealer != Owner.Creature ||
            !props.IsPoweredAttack() || (target is not null && target.Side == dealer.Side))
            return 0m;
        return LossBonus(CombatHpLossMechanics.GetLoss(Owner));
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3").Execute(context);
    }
    protected override void OnUpgrade() => DynamicVars["HpPerDamage"].UpgradeValueBy(-1m);
}
