using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class Contents() : SnowfallenCard(
    1,
    CardType.Skill,
    CardRarity.Common,
    TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
        new CardsVar("ExtraCards", 2),
        new PowerVar<WaterBreathPower>(3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        WaterBreathPower waterBreath = Owner.Creature.Powers
            .OfType<WaterBreathPower>()
            .FirstOrDefault();
        decimal required = DynamicVars["WaterBreathPower"].BaseValue;
        if ((waterBreath?.Amount ?? 0m) >= required)
        {
            await WaterBreathMechanics.Consume(
                choiceContext,
                Owner.Creature,
                required,
                this);
            await CardPileCmd.Draw(
                choiceContext,
                DynamicVars["ExtraCards"].BaseValue,
                Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
