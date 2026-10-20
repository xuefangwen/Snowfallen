using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class AzureRiver() : SnowfallenCard(
    0,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WaterBreathPower>()];

    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Innate];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new PowerVar<WaterBreathPower>(2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> candidates = PileType.Draw.GetPile(Owner).Cards.ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        int maximum = Math.Min(DynamicVars.Cards.IntValue, candidates.Count);
        List<CardModel> selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, maximum)))
            .ToList();
        foreach (CardModel card in selected)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        if (selected.Count > 0)
        {
            await WaterBreathMechanics.Gain(
                choiceContext,
                Owner.Creature,
                DynamicVars["WaterBreathPower"].BaseValue * selected.Count,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
