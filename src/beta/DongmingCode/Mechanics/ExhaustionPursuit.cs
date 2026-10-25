using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace SnowfallenMod.DongmingCode.Mechanics;

/// <summary>
/// “衰竭追击”词条：标记“对处于衰竭的敌人有额外效果”的牌。
///
/// 原版关键词（消耗 / 固有等）的规格是两件事：卡面描述末尾单独一行金色词条文字，
/// 加右侧悬停索引里的一条说明。CardKeyword 是原版枚举，没法加新值，
/// 所以这里按同样的显示规格手写，观感与“消耗”一致。
/// </summary>
public static class ExhaustionPursuit
{
    /// <summary>词条标题（cards 表）。</summary>
    public const string TitleKey = "SNOWFALLENMOD-EXHAUSTION_PURSUIT.title";

    /// <summary>卡面描述末尾单独一行的词条文字。</summary>
    public const string CardText = "[gold]衰竭追击[/gold]。";

    /// <summary>单面牌（普通牌）的说明键：&lt;牌ID&gt;.exhaustionPursuit。</summary>
    public static string KeyForCard(CardModel card) =>
        $"{card.Id.Entry}.exhaustionPursuit";

    /// <summary>
    /// 明晦牌某一面的说明键：&lt;牌ID&gt;.exhaustionPursuit.bright / .dark。
    /// </summary>
    public static string KeyForPhase(CardModel card, MoonPhase phase) =>
        $"{card.Id.Entry}.exhaustionPursuit." +
        (phase == MoonPhase.Bright ? "bright" : "dark");

    /// <summary>索引条目：标题“衰竭追击”，正文取指定说明键。</summary>
    public static IHoverTip CreateTip(CardModel card, string descriptionLocKey) =>
        new HoverTip(
            new LocString("cards", TitleKey),
            BuildDescription(card, descriptionLocKey));

    /// <summary>索引条目（单面牌）。</summary>
    public static IHoverTip CreateTip(CardModel card) =>
        CreateTip(card, KeyForCard(card));

    /// <summary>
    /// 把词条说明并进别的条目正文（明晦牌的“另一面”条目用），
    /// 这样翻到另一面时也能看到那一面的衰竭效果。
    /// </summary>
    public static string DescribeFor(CardModel card, string descriptionLocKey) =>
        $"[gold]{new LocString("cards", TitleKey).GetFormattedText()}[/gold]：" +
        BuildDescription(card, descriptionLocKey).GetFormattedText();

    private static LocString BuildDescription(
        CardModel card,
        string descriptionLocKey)
    {
        LocString description = new("cards", descriptionLocKey);
        card.DynamicVars.AddTo(description);
        return description;
    }
}

