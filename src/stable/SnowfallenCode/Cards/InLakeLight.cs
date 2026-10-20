using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class InLakeLight() : SnowfallenCard(
    0,
    CardType.Skill,
    CardRarity.Rare,
    TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WaterBreathPower>(),
        HoverTipFactory.FromPower<LakeLightEmberDamagePower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WaterBreathPower>(2m),
        new DynamicVar("EmberDamage", 3m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await WaterBreathMechanics.Gain(
            choiceContext,
            Owner.Creature,
            DynamicVars["WaterBreathPower"].BaseValue,
            Owner.Creature,
            this);

        LakeLightMaxHpPower tracker = Owner.Creature.Powers
            .OfType<LakeLightMaxHpPower>()
            .FirstOrDefault();
        if (tracker is not null && !tracker.IsUsageTracker)
        {
            // Old combat saves used this model as the pending damage buff.
            // Replace it with the new split tracker/pending-power model.
            await PowerCmd.Remove(tracker);
            tracker = null;
        }

        if (tracker is null)
        {
            await PowerCmd.Apply<LakeLightMaxHpPower>(
                choiceContext,
                Owner.Creature,
                1m,
                Owner.Creature,
                this);
            tracker = Owner.Creature.Powers
                .OfType<LakeLightMaxHpPower>()
                .First();
        }
        else
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                tracker,
                1m,
                Owner.Creature,
                this);
        }

        tracker.IsUsageTracker = true;
        decimal increase = tracker.Amount *
                           DynamicVars["EmberDamage"].BaseValue;
        LakeLightEmberDamagePower pending = Owner.Creature.Powers
            .OfType<LakeLightEmberDamagePower>()
            .FirstOrDefault();
        if (pending is not null)
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                pending,
                increase,
                Owner.Creature,
                this);
            return;
        }

        await PowerCmd.Apply<LakeLightEmberDamagePower>(
            choiceContext,
            Owner.Creature,
            increase,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["EmberDamage"].UpgradeValueBy(1m);
    }
}
