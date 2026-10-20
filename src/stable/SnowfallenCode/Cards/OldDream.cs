using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class OldDream() : SnowfallenCard(
    0,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    protected override bool IsPlayable =>
        base.IsPlayable &&
        (Owner?.Creature?.Powers.OfType<EmberTrackerPower>().FirstOrDefault()
            ?.GeneratedThisCombat ?? 0) > 0;

    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<IEmberCard> candidates = PileType.Exhaust.GetPile(Owner).Cards
            .OfType<IEmberCard>()
            .ToList();
        if (candidates.Count > 0)
        {
            IEmberCard selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates.Select(candidate => candidate.Card).ToList(),
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1)))
                .OfType<IEmberCard>()
                .FirstOrDefault();
            if (selected is not null)
            {
                EmberTrackerPower tracker = await EmberMechanics.EnsureTracker(
                    choiceContext,
                    Owner,
                    CombatState,
                    this);
                tracker.RegisterNewActiveEmber(selected);
                await CardPileCmd.Add(selected.Card, PileType.Draw, CardPilePosition.Top);
            }
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
