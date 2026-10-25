using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.DongmingCode.Cards;
using SnowfallenMod.SnowfallenCode.Cards;
using System.Reflection;

namespace SnowfallenMod.SnowfallenCode.Patches;

[HarmonyPatch(typeof(CardModel), "get_Description")]
public static class CardUpgradeDescriptionPatch
{
    private static readonly PropertyInfo VariableIndexer =
        typeof(LocString).GetProperty(
            "Item",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new MissingMemberException(typeof(LocString).FullName, "Item");

    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is Ember ember)
        {
            LocString emberDescription = new(
                "cards",
                __instance is Annotation
                    ? "SNOWFALLENMOD-ANNOTATION.description"
                    : "SNOWFALLENMOD-EMBER.description");
            emberDescription.AddVariablesFrom(__result);
            SetVariable(emberDescription, "DamageTarget", ember.IsAoe
                ? "对所有敌人"
                : string.Empty);
            ApplyFusionEffectLines(
                emberDescription,
                ember.ExhaustBlock,
                ember.VulnerableOnPlay,
                ember.WeakOnPlay,
                ember.AppliesTwist,
                ember.ExtraDrawOnPlay,
                ember.ReplayCount);
            SetVariable(emberDescription, "FusionSubjectEffect",
                ember.IsFusionSubject
                    ? "\n【融合主体】本次选择的弃牌堆余烬会依次消耗并融合到此牌。"
                    : string.Empty);
            __result = emberDescription;
            return;
        }

        if (__instance is WordsFlying wordsFlying)
        {
            LocString wordsFlyingDescription = new(
                "cards",
                "SNOWFALLENMOD-WORDS_FLYING.description");
            wordsFlyingDescription.AddVariablesFrom(__result);
            ApplyFusionEffectLines(
                wordsFlyingDescription,
                wordsFlying.PlayBlock,
                wordsFlying.VulnerableOnPlay,
                wordsFlying.WeakOnPlay,
                wordsFlying.AppliesTwist,
                wordsFlying.ExtraDrawOnPlay,
                wordsFlying.ReplayCount);
            SetVariable(wordsFlyingDescription, "FusionSubjectEffect",
                wordsFlying.IsFusionSubject
                    ? "\n【融合主体】本次选择的消耗牌堆余烬会依次融合到此牌。"
                    : string.Empty);
            __result = wordsFlyingDescription;
            return;
        }

        if (!__instance.IsUpgraded)
        {
            return;
        }

        string locEntryKey = __instance switch
        {
            PickUpFirewood => "SNOWFALLENMOD-PICK_UP_FIREWOOD.description.upgraded",
            BurnBook => "SNOWFALLENMOD-BURN_BOOK.description.upgraded",
            AzureRiver => "SNOWFALLENMOD-AZURE_RIVER.description.upgraded",
            Preface => "SNOWFALLENMOD-PREFACE.description.upgraded",
            AcceptMandate => "SNOWFALLENMOD-ACCEPT_MANDATE.description.upgraded",
            LiaoYue => "SNOWFALLENMOD-LIAO_YUE.description.upgraded",
            JinChangJie => "SNOWFALLENMOD-JIN_CHANG_JIE.description.upgraded",
            CengNing => "SNOWFALLENMOD-CENG_NING.description.upgraded",
            EJie => "SNOWFALLENMOD-E_JIE.description.upgraded",
            _ => string.Empty
        };
        if (string.IsNullOrEmpty(locEntryKey))
        {
            return;
        }

        LocString replacement = new("cards", locEntryKey);
        replacement.AddVariablesFrom(__result);
        __result = replacement;
    }

    /// <summary>
    /// Builds the ember effect lines in the agreed order: block, debuffs
    /// (vulnerable/weak/twist), draw, then replay. Each line carries its own
    /// leading newline so an absent effect leaves no blank row behind.
    /// </summary>
    private static void ApplyFusionEffectLines(
        LocString description,
        decimal block,
        int vulnerable,
        int weak,
        bool appliesTwist,
        int extraDraw,
        int replayCount)
    {
        SetVariable(description, "BlockEffect", block > 0m
            ? $"\n获得{block:0}点格挡。"
            : string.Empty);

        List<string> debuffs = [];
        if (vulnerable > 0)
        {
            debuffs.Add($"{vulnerable}层易伤");
        }
        if (weak > 0)
        {
            debuffs.Add($"{weak}层虚弱");
        }
        if (appliesTwist)
        {
            debuffs.Add("1层扭转");
        }
        SetVariable(description, "DebuffEffect", debuffs.Count > 0
            ? $"\n施加{string.Join("、", debuffs)}。"
            : string.Empty);

        SetVariable(description, "DrawEffect", extraDraw > 0
            ? $"\n打出后抽{extraDraw}张牌。"
            : string.Empty);

        SetVariable(description, "ReplayEffect", replayCount > 0
            ? $"\n重放{replayCount}。"
            : string.Empty);
    }

    private static void SetVariable(
        LocString locString,
        string name,
        object value)
    {
        VariableIndexer.SetValue(locString, value, [name]);
    }
}

