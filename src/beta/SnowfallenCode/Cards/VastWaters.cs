using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Powers;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class VastWaters() : SnowfallenCard(
    0,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<NextAttackDiscountPower>()
        , BloodThresholdHoverTip()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(3),
        new DynamicVar("Discount", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> topCards = PileType.Draw.GetPile(Owner).Cards
            .Take(DynamicVars.Cards.IntValue)
            .ToList();

        if (topCards.Count > 0)
        {
            CardModel selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                topCards,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
            if (selected is not null)
            {
                await CardPileCmd.Add(selected, PileType.Hand);
            }
        }

        if (BloodThresholdMechanics.IsHigh(Owner.Creature))
        {
            await PowerCmd.Apply<NextAttackDiscountPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["Discount"].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

