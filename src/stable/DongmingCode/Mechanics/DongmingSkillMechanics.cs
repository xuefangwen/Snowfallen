using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.DongmingCode.Cards;
using SnowfallenMod.DongmingCode.Powers;

namespace SnowfallenMod.DongmingCode.Mechanics;

public static class DongmingSkillMechanics
{
    private const int XuShiYinSelectionCap = 10;

    public static async Task<CanShi> CreateCanShi(
        PlayerChoiceContext choiceContext,
        Player owner,
        CardModel source)
    {
        CanShi card = ((CombatState)owner.Creature.CombatState)
            .CreateCard<CanShi>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(
            card,
            PileType.Hand,
            owner,
            CardPilePosition.Bottom);
        return card;
    }

    public static async Task CreateMoonlight(
        Player owner,
        PileType pile,
        int count,
        bool upgraded,
        CardPilePosition position = CardPilePosition.Bottom)
    {
        for (int i = 0; i < count; i++)
        {
            YueHua card = ((CombatState)owner.Creature.CombatState)
                .CreateCard<YueHua>(owner);
            if (upgraded)
            {
                CardCmd.Upgrade(card);
            }
            await CardPileCmd.AddGeneratedCardToCombat(
                card, pile, owner, position);
        }
    }

    public static async Task SelectAndToggle(
        PlayerChoiceContext choiceContext,
        Player owner,
        AbstractModel source,
        int maximum,
        bool consumeMark = false)
    {
        List<CardModel> candidates = PileType.Hand.GetPile(owner).Cards
            .Where(card => !ReferenceEquals(card, source))
            .Where(PurificationMechanics.IsTransformable)
            .ToList();
        int max = Math.Min(
            Math.Min(maximum, XuShiYinSelectionCap),
            candidates.Count);
        if (max <= 0)
            return;

        string locTable = source is XuShiYinPower ? "powers" : "cards";
        CardSelectorPrefs prefs = new(
            new LocString(locTable, $"{source.Id.Entry}.selectionScreenPrompt"),
            0,
            max)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
        List<CardModel> selected = (await CardSelectCmd.FromHand(
            choiceContext,
            owner,
            prefs,
            candidates.Contains,
            source)).Distinct().ToList();
        foreach (CardModel card in selected)
        {
            await PurificationMechanics.Transform(choiceContext, card);
            if (consumeMark)
            {
                XuShiYinPower mark = owner.Creature.Powers
                    .OfType<XuShiYinPower>()
                    .FirstOrDefault();
                if (mark is not null)
                    await PowerCmd.ModifyAmount(
                        choiceContext, mark, -1m, owner.Creature,
                        source as CardModel);
            }
        }
    }

    /// <summary>
    /// "立即触发虚实印"的统一入口：可选取上限取**当前总层数**（含这张牌/这瓶药
    /// 刚刚加上的层数，内部仍受 XuShiYinSelectionCap 与手牌可转化数限制），
    /// 与回合开始时由 XuShiYinPower 自己发起的触发口径保持一致。
    /// 之前各调用点传的是"本牌刚给的层数"，导致身上已有存量时反而被卡住。
    /// </summary>
    public static async Task SelectAndToggleWithCurrentMarks(
        PlayerChoiceContext choiceContext,
        Player owner,
        AbstractModel source,
        bool consumeMark = true)
    {
        XuShiYinPower marks = owner.Creature.Powers
            .OfType<XuShiYinPower>()
            .FirstOrDefault();
        if (marks is null)
        {
            return;
        }

        await SelectAndToggle(
            choiceContext,
            owner,
            source,
            (int)marks.Amount,
            consumeMark);
    }
}
