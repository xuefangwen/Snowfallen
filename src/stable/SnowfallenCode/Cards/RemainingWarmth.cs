using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

// 注意：烬衣与余温的"实际效果"已按需求整体对调——卡名、卡面、费用、稀有度不变，
// 所以余温现在执行原本烬衣的效果（从弃牌堆取牌并给予活力），烬衣执行原本余温的效果。
public class RemainingWarmth() : SnowfallenCard(
    1,
    CardType.Skill,
    CardRarity.Common,
    TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VigorPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new ExtraDamageVar(3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> candidates = PileType.Discard
            .GetPile(Owner)
            .Cards
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        int maximum = Math.Min(DynamicVars.Cards.IntValue, candidates.Count);
        List<CardModel> selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates,
                Owner,
                new CardSelectorPrefs(
                    SelectionScreenPrompt,
                    0,
                    maximum)))
            .ToList();

        foreach (CardModel card in selected)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }

        if (selected.Count > 0)
        {
            await PowerCmd.Apply<VigorPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars.ExtraDamage.BaseValue * selected.Count,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
