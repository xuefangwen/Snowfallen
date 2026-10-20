using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Powers;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class Glimmer() : SnowfallenCard(
    3,
    CardType.Power,
    CardRarity.Rare,
    TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Replay", 1m),
        new BlockVar(2m, ValueProp.Unpowered)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        IEnumerable<IEmberCard> existingEmbers = new[]
            {
                PileType.Hand,
                PileType.Draw,
                PileType.Discard,
                PileType.Exhaust
            }
            .SelectMany(type => type.GetPile(Owner).Cards)
            .OfType<IEmberCard>()
            .Distinct();
        foreach (IEmberCard ember in existingEmbers)
        {
            ember.AddReplayBonus(DynamicVars["Replay"].IntValue);
            ember.AddPermanentBlockBonus(DynamicVars.Block.BaseValue);
        }

        if (IsUpgraded)
        {
            await PowerCmd.Apply<UpgradedGlimmerPower>(
                choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        }
        else
        {
            await PowerCmd.Apply<GlimmerPower>(
                choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1m);
    }
}
