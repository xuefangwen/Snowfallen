using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class FoldedPage() : SnowfallenCard(
    0,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        if (!PileType.Hand.GetPile(Owner).Cards.Any(card =>
                !card.EnergyCost.CostsX &&
                card.EnergyCost.GetWithModifiers(CostModifiers.All) >= 2))
        {
            return;
        }

        CardModel selected = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1, 1),
            card => !card.EnergyCost.CostsX &&
                    card.EnergyCost.GetWithModifiers(CostModifiers.All) >= 2,
            this)).FirstOrDefault();
        if (selected is null)
        {
            return;
        }

        selected.EnergyCost.AddThisCombat(-1, reduceOnly: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

