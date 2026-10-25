using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.DongmingCode.Cards;
using SnowfallenMod.DongmingCode.Character;
using SnowfallenMod.DongmingCode.Mechanics;

namespace SnowfallenMod.DongmingCode.Patches;

/// <summary>
/// 原版只在 CardCmd.Discard() 里调用 Hook.AfterCardDiscarded，
/// 而"回合结束自动弃手牌"是直接走 CardPileCmd.Add 到弃牌堆的，
/// 因此利息的"+5 金币/次"从来没被触发过。
/// 这里改为按"卡牌进入弃牌堆"计数（覆盖所有进入弃牌堆的路径）。
/// 注意：Dongming.AfterCardDiscarded 里对应的计数已移除，避免重复 +5。
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add),
    new[] { typeof(CardModel), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool) })]
internal static class DebtDiscardPilePatch
{
    [HarmonyPrefix]
    private static void Prefix(CardModel card, CardPile newPile)
        => DebtDiscardPilePatchHelper.Handle(card, newPile?.Type);
}

[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add),
    new[] { typeof(CardModel), typeof(PileType), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool) })]
internal static class DebtDiscardPileTypePatch
{
    [HarmonyPrefix]
    private static void Prefix(CardModel card, PileType newPileType)
        => DebtDiscardPilePatchHelper.Handle(card, newPileType);
}

[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add),
    new[] { typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool), typeof(bool) })]
internal static class DebtDiscardPileBulkPatch
{
    [HarmonyPrefix]
    private static void Prefix(IEnumerable<CardModel> cards, CardPile newPile)
        => DebtDiscardPilePatchHelper.HandleMany(cards, newPile?.Type);
}

[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add),
    new[] { typeof(IEnumerable<CardModel>), typeof(PileType), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool) })]
internal static class DebtDiscardPileTypeBulkPatch
{
    [HarmonyPrefix]
    private static void Prefix(IEnumerable<CardModel> cards, PileType newPileType)
        => DebtDiscardPilePatchHelper.HandleMany(cards, newPileType);
}

internal static class DebtDiscardPilePatchHelper
{
    // 记录"当前已经在弃牌堆里、且已计过数"的牌，避免批量重载与单卡重载
    // 层层调用导致同一张牌被计两次；离开弃牌堆时移除，允许下次再计。
    private static readonly HashSet<CardModel> CountedInDiscard =
        new(ReferenceEqualityComparer.Instance);

    internal static void HandleMany(IEnumerable<CardModel> cards, PileType? target)
    {
        if (cards is null)
        {
            return;
        }

        foreach (CardModel card in cards)
        {
            Handle(card, target);
        }
    }

    internal static void Handle(CardModel card, PileType? target)
    {
        if (card is not PurifiedDebt debt || target is null)
        {
            return;
        }

        if (target != PileType.Discard)
        {
            // 离开弃牌堆（抽回手牌 / 被消耗等）
            CountedInDiscard.Remove(card);
            return;
        }

        if (!CountedInDiscard.Add(card))
        {
            return;   // 本次进入已经计过数
        }

        if (debt.Owner?.Character is not Dongming)
        {
            return;
        }

        _ = PurifiedVanillaEffects.RecordDebtDiscard(
            new BlockingPlayerChoiceContext(),
            debt);
    }
}

