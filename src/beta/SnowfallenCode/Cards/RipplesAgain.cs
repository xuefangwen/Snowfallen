using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class RipplesAgain() : SnowfallenCard(
    1,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
        new ExtraDamageVar(3m),
        new DynamicVar("Weak", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<IEmberCard> candidates = PileType.Exhaust.GetPile(Owner).Cards
            .OfType<IEmberCard>()
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        List<IEmberCard> selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates.Select(candidate => candidate.Card).ToList(),
                Owner,
                new CardSelectorPrefs(
                    SelectionScreenPrompt,
                    0,
                    Math.Min(DynamicVars.Cards.IntValue, candidates.Count))))
            .OfType<IEmberCard>()
            .ToList();
        foreach (IEmberCard ember in selected)
        {
            ember.AddPermanentDamageBonus(DynamicVars.ExtraDamage.BaseValue);
            ember.AddWeakOnExhaust((int)DynamicVars["Weak"].BaseValue);
            await CardPileCmd.Add(ember.Card, PileType.Hand);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Weak"].UpgradeValueBy(1m);
    }
}

