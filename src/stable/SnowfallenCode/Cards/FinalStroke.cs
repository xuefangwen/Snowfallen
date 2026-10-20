using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class FinalStroke() : SnowfallenCard(
    2,
    CardType.Power,
    CardRarity.Rare,
    TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("GrowthBonus", 4m),
        new CardsVar("EmberThreshold", 2),
        new EnergyVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        EmberTrackerPower tracker = await EmberMechanics.EnsureTracker(
            choiceContext,
            Owner,
            CombatState,
            this);
        tracker.AddGlobalPlayGrowthBonus(
            DynamicVars["GrowthBonus"].BaseValue);

        await PowerCmd.Apply<FinalStrokePower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["GrowthBonus"].UpgradeValueBy(3m);
    }
}
