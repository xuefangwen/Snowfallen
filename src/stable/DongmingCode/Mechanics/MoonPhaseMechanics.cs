using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace SnowfallenMod.DongmingCode.Mechanics;

public enum MoonPhase
{
    Bright,
    Dark
}

public interface IMoonPhaseCard
{
    MoonPhase InitialMoonPhase { get; }
    MoonPhase CurrentMoonPhase { get; }

    void ToggleMoonPhase();
    void ResetMoonPhase();
}

public interface ICombatPhaseFlipListener
{
    Task OnCombatPhaseFlipped(
        MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext
            choiceContext);
}

public static class MoonPhaseMechanics
{
    private static readonly Action<NCard> ReloadCardNode =
        AccessTools.MethodDelegate<Action<NCard>>(
            AccessTools.Method(typeof(NCard), "Reload"));

    private static readonly FieldInfo CardHolderHoveredField =
        AccessTools.Field(typeof(NCardHolder), "_isHovered");

    private static readonly Dictionary<Type, MethodInfo> CreateHoverTipsMethods = [];

    public static bool IsSupported(CardModel card) =>
        card is IMoonPhaseCard;

    public static bool IsBright(CardModel card) =>
        card is IMoonPhaseCard moonPhaseCard &&
        moonPhaseCard.CurrentMoonPhase == MoonPhase.Bright;

    public static bool IsBright(IMoonPhaseCard card) =>
        card.CurrentMoonPhase == MoonPhase.Bright;

    public static MoonPhase GetPhase(CardModel card) =>
        card is IMoonPhaseCard moonPhaseCard
            ? moonPhaseCard.CurrentMoonPhase
            : MoonPhase.Dark;

    public static void Toggle(IMoonPhaseCard card)
    {
        card.ToggleMoonPhase();
    }

    public static async Task ToggleAsync(
        CardModel card,
        MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext
            choiceContext)
    {
        if (card is IMoonPhaseCard moonPhaseCard)
        {
            moonPhaseCard.ToggleMoonPhase();
            await NotifyCombatPhaseFlip(card, choiceContext);
        }
    }

    public static async Task NotifyCombatPhaseFlip(
        CardModel flippedCard,
        MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext
            choiceContext)
    {
        if (flippedCard.Owner is null)
        {
            return;
        }

        foreach (ICombatPhaseFlipListener listener in flippedCard.Owner
                     .PlayerCombatState.AllCards
                     .OfType<ICombatPhaseFlipListener>()
                     .ToList())
        {
            await listener.OnCombatPhaseFlipped(choiceContext);
        }
    }

    public static void ResetAll(
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        foreach (IMoonPhaseCard card in player.PlayerCombatState.AllCards
            .OfType<IMoonPhaseCard>())
        {
            card.ResetMoonPhase();
        }
    }

    public static IHoverTip CreateHoverTip(IMoonPhaseCard card)
    {
        return CreateHoverTip(card, card.CurrentMoonPhase);
    }

    public static IHoverTip CreateHoverTip(
        IMoonPhaseCard card,
        MoonPhase phase)
    {
        string descriptionKey = phase == MoonPhase.Bright
            ? "SNOWFALLENMOD-DONGMING_MOON_PHASE.bright"
            : "SNOWFALLENMOD-DONGMING_MOON_PHASE.dark";
        return new HoverTip(
            new LocString("cards", "SNOWFALLENMOD-DONGMING_MOON_PHASE.title"),
            new LocString("cards", descriptionKey));
    }

    public static void RefreshCardVisuals(CardModel card)
    {
        NCard cardNode = NCard.FindOnTable(card);
        if (cardNode is not null)
        {
            // UpdateVisuals refreshes text and costs but does not reload the
            // portrait texture. Reload first so an in-hand phase change uses
            // the new bright/dark portrait immediately.
            ReloadCardNode(cardNode);
            cardNode.UpdateVisuals(
                card.Pile?.Type ?? PileType.Hand,
                CardPreviewMode.Normal);
            RefreshHoverTips(cardNode);
        }
    }

    /// <summary>
    /// 悬停状态下翻转明晦时，右侧的悬停索引也要跟着换成另一面的内容。
    /// 没有悬停就不处理，避免凭空弹出提示框。
    /// </summary>
    private static void RefreshHoverTips(NCard cardNode)
    {
        NCardHolder holder = FindCardHolder(cardNode);
        if (holder is null || holder.CardNode is null)
        {
            return;
        }

        if (CardHolderHoveredField?.GetValue(holder) is not true)
        {
            return;
        }

        // 先移除旧提示再重建，否则 NHoverTipSet 内部会因重复登记而报错。
        NHoverTipSet.Remove(holder);
        MethodInfo createHoverTips = ResolveCreateHoverTips(holder.GetType());
        if (createHoverTips is not null)
        {
            createHoverTips.Invoke(holder, null);
            return;
        }

        // 兜底：退回公开 API，效果与基类实现一致。
        NHoverTipSet.CreateAndShow(holder, cardNode.Model.HoverTips)
            ?.SetAlignmentForCardHolder(holder);
    }

    private static NCardHolder FindCardHolder(NCard cardNode)
    {
        Node node = cardNode.GetParent();
        while (node is not null)
        {
            if (node is NCardHolder holder)
            {
                return holder;
            }

            node = node.GetParent();
        }

        return null;
    }

    /// <summary>
    /// 用实际运行类型取 CreateHoverTips，保证走派生类重写的版本
    /// （手牌 / 预览 / 选中等 holder 的实现不完全一样）。
    /// </summary>
    private static MethodInfo ResolveCreateHoverTips(Type holderType)
    {
        if (!CreateHoverTipsMethods.TryGetValue(holderType, out MethodInfo method))
        {
            method = AccessTools.Method(holderType, "CreateHoverTips");
            CreateHoverTipsMethods[holderType] = method;
        }

        return method;
    }
}
