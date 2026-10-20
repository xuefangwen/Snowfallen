using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class BurnBook() : SnowfallenCard(
    1,
    CardType.Skill,
    CardRarity.Common,
    TargetType.Self)
{
    protected override bool IsPlayable => PileType.Draw.GetPile(Owner).Cards.Count > 0;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> candidates = PileType.Draw.GetPile(Owner).Cards.ToList();
        int maximum = Math.Min(DynamicVars.Cards.IntValue, candidates.Count);
        List<CardModel> selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            new CardSelectorPrefs(
                SelectionScreenPrompt,
                1,
                maximum))).ToList();
        foreach (CardModel card in selected)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        await EmberMechanics.CreateInHand(Owner, CombatState, selected.Count);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

