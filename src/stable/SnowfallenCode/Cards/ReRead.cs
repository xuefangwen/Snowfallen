using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class ReRead() : SnowfallenCard(
    2,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new RepeatVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardSelectorPrefs prefs = new(SelectionScreenPrompt, 1)
        {
            PretendCardsCanBePlayed = true
        };
        CardModel selected = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            prefs,
            card => !card.Keywords.Contains(CardKeyword.Unplayable),
            this)).FirstOrDefault();
        if (selected is null)
        {
            return;
        }

        List<CardModel> copies = [];
        for (int i = 0; i < DynamicVars.Repeat.IntValue; i++)
        {
            CardModel copy = selected.CreateClone();
            copy.EnergyCost.SetThisCombat(0);
            CardCmd.ApplyKeyword(copy, CardKeyword.Exhaust);
            copies.Add(copy);
        }

        IEmberCard selectedEmber = selected as IEmberCard;
        EmberTrackerPower emberTracker = null;
        if (selectedEmber is not null)
        {
            emberTracker = await EmberMechanics.EnsureTracker(
                choiceContext,
                Owner,
                CombatState,
                this);
            emberTracker.ReserveFromFusion([selectedEmber]);
        }

        try
        {
            await CardCmd.Exhaust(choiceContext, selected);
            if (selectedEmber is not null)
            {
                emberTracker.ReleaseFromFusion(selectedEmber);
            }
            foreach (CardModel copy in copies)
            {
                if (copy is IEmberCard ember)
                {
                    emberTracker.RegisterNewActiveEmber(ember);
                }
                await CardPileCmd.AddGeneratedCardToCombat(
                    copy,
                    PileType.Play,
                    Owner);
                await CardCmd.AutoPlay(choiceContext, copy, null);
            }

        }
        finally
        {
            if (selectedEmber is not null)
            {
                emberTracker?.ReleaseFromFusion(selectedEmber);
            }
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
