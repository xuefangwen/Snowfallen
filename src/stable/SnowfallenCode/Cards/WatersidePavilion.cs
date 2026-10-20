using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class WatersidePavilion() : SnowfallenCard(
    0,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [
            HoverTipFactory.FromPower<WaterBreathPower>(),
            HoverTipFactory.FromPower<FrostPower>()
        ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WaterBreathPower>(5m),
        new EnergyVar(2),
        new PowerVar<FrostPower>(3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal required = DynamicVars["WaterBreathPower"].BaseValue;
        decimal available = Owner.Creature.Powers
            .OfType<WaterBreathPower>()
            .FirstOrDefault()?.Amount ?? 0m;
        if (available >= required)
        {
            await WaterBreathMechanics.Consume(
                choiceContext,
                Owner.Creature,
                required,
                this);
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        }
        else
        {
            await FrostMechanics.Gain(
                choiceContext,
                Owner.Creature,
                DynamicVars["FrostPower"].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["FrostPower"].UpgradeValueBy(2m);
    }
}
