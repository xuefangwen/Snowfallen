using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Logging;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Extensions;
using SnowfallenMod.DongmingCode.CardPools;

namespace SnowfallenMod.DongmingCode.Cards;

[Pool(typeof(DongmingCardPool))]
public abstract class DongmingCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    public override string CustomPortraitPath
    {
        get
        {
            string path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : PortraitPath;
        }
    }

    public override string PortraitPath
    {
        get
        {
            string path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : "card.png".CardImagePath();
        }
    }

    public override string BetaPortraitPath
    {
        get
        {
            string path = $"Beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : PortraitPath;
        }
    }
}

public abstract class MoonPhaseCard(
    int cost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    MoonPhase initialMoonPhase) :
    DongmingCard(cost, type, rarity, target),
    IMoonPhaseCard
{
    private MoonPhase _currentMoonPhase = initialMoonPhase;

    public MoonPhase InitialMoonPhase { get; } = initialMoonPhase;

    [SavedProperty]
    public MoonPhase CurrentMoonPhase
    {
        get => _currentMoonPhase;
        protected set
        {
            AssertMutable();
            _currentMoonPhase = value;
        }
    }

    protected virtual IEnumerable<IHoverTip> AdditionalMoonPhaseHoverTips =>
        Array.Empty<IHoverTip>();

    private string MoonPhasePortraitFileName
    {
        get
        {
            string modelName = Id.Entry.RemovePrefix().ToLowerInvariant();
            return CurrentMoonPhase == MoonPhase.Bright
                ? $"{modelName}_bright.png"
                : $"{modelName}.png";
        }
    }

    public override string CustomPortraitPath
    {
        get
        {
            string path = MoonPhasePortraitFileName.BigCardImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : base.CustomPortraitPath;
        }
    }

    public override string PortraitPath
    {
        get
        {
            string path = MoonPhasePortraitFileName.CardImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : base.PortraitPath;
        }
    }

    /// <summary>
    /// 为 true 时，卡面只显示当前月相的效果，另一面的效果改为放进右侧的
    /// 悬停索引里显示（战斗翻转时会同步换面）。
    /// 东铭的明晦牌（含自制诅咒牌）全部打开。
    /// </summary>
    protected virtual bool ShowOtherPhaseInHoverTipOnly => false;

    /// <summary>
    /// 指定那一面是否有“衰竭追击”说明
    /// （键：&lt;牌ID&gt;.exhaustionPursuit.bright / .dark）。
    /// 咒击 / 噩咒只有明面有，谴灾两面都有；返回 false 的一面不显示该词条。
    /// </summary>
    protected virtual bool HasExhaustionPursuit(MoonPhase phase) => false;

    protected virtual bool ShowExhaustionPursuitInPhaseDescription => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            // 这里必须当场把整份提示生成完：悬停提示是延迟枚举的，
            // 若在枚举途中翻面，会出现“月相说这一面、衰竭追击说另一面”的错位。
            MoonPhase phase = CurrentMoonPhase;
            List<IHoverTip> tips = [MoonPhaseMechanics.CreateHoverTip(this, phase)];
            if (ShowOtherPhaseInHoverTipOnly)
            {
                tips.Add(CreateOtherPhaseHoverTip(phase));
            }

            bool pursuitOnCurrentPhase = HasExhaustionPursuit(phase);
            if (pursuitOnCurrentPhase)
            {
                tips.Add(ExhaustionPursuit.CreateTip(
                    this,
                    ExhaustionPursuit.KeyForPhase(this, phase)));
            }

            tips.AddRange(AdditionalMoonPhaseHoverTips);
            return tips;
        }
    }

    public void ToggleMoonPhase()
    {
        SetMoonPhase(CurrentMoonPhase == MoonPhase.Bright
            ? MoonPhase.Dark
            : MoonPhase.Bright);
    }

    public void ResetMoonPhase()
    {
        SetMoonPhase(InitialMoonPhase);
    }

    protected void SetMoonPhase(MoonPhase phase)
    {
        if (CurrentMoonPhase == phase)
        {
            return;
        }

        CurrentMoonPhase = phase;
        MoonPhaseMechanics.RefreshCardVisuals(this);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        LocString brightDescription = new("cards", BrightDescriptionKey);
        LocString darkDescription = new(
            "cards",
            DarkDescriptionKey);
        DynamicVars.AddTo(brightDescription);
        DynamicVars.AddTo(darkDescription);

        bool brightIsCurrent = CurrentMoonPhase == MoonPhase.Bright;
        if (ShowOtherPhaseInHoverTipOnly)
        {
            string currentText = (brightIsCurrent
                ? brightDescription
                : darkDescription).GetFormattedText();
            string faceText = Type == CardType.Curse
                ? $"[gold]当前效果[/gold]：{StripCurseEffectLabel(currentText)}"
                : FormatPhaseLine(
                    currentText,
                    brightIsCurrent ? "明" : "晦",
                    brightIsCurrent ? "gold" : "purple",
                    isCurrent: true);
            if (ShowExhaustionPursuitInPhaseDescription && HasExhaustionPursuit(CurrentMoonPhase))
            {
                faceText = $"{faceText}\n{ExhaustionPursuit.CardText}";
            }

            description.Add("MoonPhaseDescription", faceText);
            return;
        }

        if (Type == CardType.Curse)
        {
            string brightEffect = StripCurseEffectLabel(
                brightDescription.GetFormattedText());
            string darkEffect = StripCurseEffectLabel(
                darkDescription.GetFormattedText());
            string currentEffect = brightIsCurrent ? brightEffect : darkEffect;
            string purifiedEffect = brightIsCurrent ? darkEffect : brightEffect;
            description.Add(
                "MoonPhaseDescription",
                $"[gold]当前效果[/gold]：{currentEffect}\n" +
                $"[purple]净化效果[/purple]：{purifiedEffect}");
            return;
        }

        string brightText = FormatPhaseLine(
            brightDescription.GetFormattedText(),
            "明",
            "gold",
            brightIsCurrent);
        string darkText = FormatPhaseLine(
            darkDescription.GetFormattedText(),
            "晦",
            "purple",
            !brightIsCurrent);
        description.Add(
            "MoonPhaseDescription",
            $"{brightText}\n{darkText}");
    }

    private static string StripCurseEffectLabel(string text)
    {
        string[] prefixes =
        [
            "[gold]当前效果[/gold]：",
            "[gold]净化效果[/gold]：",
            "[purple]当前效果[/purple]：",
            "[purple]净化效果[/purple]："
        ];
        foreach (string prefix in prefixes)
        {
            if (text.StartsWith(prefix, StringComparison.Ordinal))
            {
                return text[prefix.Length..];
            }
        }

        return text;
    }

    private static string FormatPhaseLine(
        string text,
        string phaseName,
        string color,
        bool isCurrent)
    {
        string coloredLabel = $"[{color}]{phaseName}[/{color}]";
        string replacement = isCurrent
            ? $"[{color}]{phaseName}（当前）[/{color}]"
            : phaseName;
        return text.Replace(coloredLabel, replacement, StringComparison.Ordinal);
    }

    protected virtual string BrightDescriptionKey =>
        $"{Id.Entry}.description.bright";

    protected virtual string DarkDescriptionKey =>
        $"{Id.Entry}.description.dark";

    /// <summary>
    /// 另一面的效果，作为右侧悬停索引的一条显示。
    /// 注意：这里不能碰 Owner（图鉴用的是 canonical 实例）。
    /// </summary>
    private IHoverTip CreateOtherPhaseHoverTip(MoonPhase currentPhase)
    {
        bool otherIsBright = currentPhase != MoonPhase.Bright;
        MoonPhase otherPhase = otherIsBright
            ? MoonPhase.Bright
            : MoonPhase.Dark;
        LocString description = new(
            "cards",
            otherIsBright
                ? BrightDescriptionKey
                : DarkDescriptionKey);
        DynamicVars.AddTo(description);
        string body = StripPhaseLabel(description.GetFormattedText());
        if (HasExhaustionPursuit(otherPhase) &&
            !HasExhaustionPursuit(currentPhase))
        {
            // 当前面本身没有“衰竭追击”条目（例如咒击 / 噩咒的晦面），
            // 就在“另一面”里补上那一面的衰竭说明，免得看不到细节；
            // 两面都有的牌（谴灾）由单独的“衰竭追击”条目负责，不重复。
            body = $"{body}\n" +
                   ExhaustionPursuit.DescribeFor(
                       this,
                       ExhaustionPursuit.KeyForPhase(this, otherPhase));
        }

        string titleKey = otherIsBright
            ? "SNOWFALLENMOD-DONGMING_MOON_PHASE.otherSide.bright"
            : "SNOWFALLENMOD-DONGMING_MOON_PHASE.otherSide.dark";
        return new HoverTip(
            new LocString("cards", titleKey),
            body);
    }

    private static string StripPhaseLabel(string text)
    {
        string[] prefixes =
        [
            "[gold]明[/gold]：",
            "[gold]晦[/gold]：",
            "[purple]明[/purple]：",
            "[purple]晦[/purple]："
        ];
        foreach (string prefix in prefixes)
        {
            if (text.StartsWith(prefix, StringComparison.Ordinal))
            {
                return text[prefix.Length..];
            }
        }

        return text;
    }
}

