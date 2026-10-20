using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class Preface() : SnowfallenCard(
    0,
    CardType.Skill,
    CardRarity.Rare,
    TargetType.Self)
{
    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel selected = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            null,
            this)).FirstOrDefault();
        if (selected is null)
        {
            return;
        }

        EmberTrackerPower tracker = null;
        int copies = Math.Max(1, DynamicVars.Cards.IntValue);
        for (int index = 0; index < copies; index++)
        {
            CardModel copy = selected.CreateClone();
            // 复制品费用固定：未升级 1 费，升级后 0 费（与本体费用无关）。
            copy.EnergyCost.SetThisCombat(IsUpgraded ? 0 : 1);
            await CardPileCmd.AddGeneratedCardToCombat(
                copy,
                PileType.Hand,
                Owner,
                CardPilePosition.Random);
            if (copy is IEmberCard ember)
            {
                tracker ??= await EmberMechanics.EnsureTracker(
                    choiceContext,
                    Owner,
                    CombatState,
                    this);
                tracker.RegisterNewActiveEmber(ember);
            }
        }
    }

    protected override void OnUpgrade()
    {
    }
}
