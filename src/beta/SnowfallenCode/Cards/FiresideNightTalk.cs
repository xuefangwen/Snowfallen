using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class FiresideNightTalk() : SnowfallenCard(
    1,
    CardType.Skill,
    CardRarity.Rare,
    TargetType.Self)
{
    protected override bool IsPlayable =>
        EmberMechanics.Count(Owner, PileType.Exhaust) >= DynamicVars["Threshold"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
        new EnergyVar(2),
        new DynamicVar("Threshold", 4m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await EmberMechanics.CreateInHand(Owner, CombatState, DynamicVars.Cards.IntValue);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Threshold"].UpgradeValueBy(-1m);
    }
}

