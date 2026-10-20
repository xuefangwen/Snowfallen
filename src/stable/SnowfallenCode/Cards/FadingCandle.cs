using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.HoverTips;
using SnowfallenMod.SnowfallenCode.Powers;
using SnowfallenMod.SnowfallenCode.Mechanics;
using MegaCrit.Sts2.Core.ValueProps;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class FadingCandle() : SnowfallenCard(
    1,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        BloodThresholdHoverTip(),
        HoverTipFactory.FromPower<BufferPower>(),
        HoverTipFactory.FromPower<TwistPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(15m, ValueProp.Move),
        new PowerVar<BufferPower>(1m),
        new PowerVar<TwistPower>(2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        bool isLowHp = BloodThresholdMechanics.IsLow(Owner.Creature);
        bool isHighOrNormalHp =
            BloodThresholdMechanics.IsHigh(Owner.Creature) ||
            BloodThresholdMechanics.IsNormal(Owner.Creature);
        if (isHighOrNormalHp)
        {
            decimal hpLoss = Math.Floor(Owner.Creature.MaxHp * 0.30m);
            // This is an explicit life payment, not incoming damage. Setting
            // current HP directly keeps existing Buffer stacks intact.
            await CreatureCmd.SetCurrentHp(
                Owner.Creature,
                Math.Max(1m, Owner.Creature.CurrentHp - hpLoss));
            if (!Owner.Creature.IsDead)
            {
                await PowerCmd.Apply<BufferPower>(
                    choiceContext,
                    Owner.Creature,
                    DynamicVars["BufferPower"].BaseValue,
                    Owner.Creature,
                    this);
            }
        }
        if (isLowHp)
        {
            IReadOnlyList<MegaCrit.Sts2.Core.Entities.Creatures.Creature> targets =
                CombatState.HittableEnemies.ToList();
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .TargetingAllOpponents(CombatState)
                .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
                .Execute(choiceContext);
            foreach (var target in targets.Where(target => !target.IsDead))
            {
                await PowerCmd.Apply<TwistPower>(
                    choiceContext,
                    target,
                    DynamicVars["TwistPower"].BaseValue,
                    Owner.Creature,
                    this);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m);
    }
}
