using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Enchantments;

namespace SnowfallenMod.SnowfallenCode.Patches;

/// <summary>
/// 「批注」附魔的悬停提示接管：
///
/// 1) 原版附魔自带的提示是普通 HoverTip（没挂模型，点了不会打开索引），而且原版
///    EnchantmentModel.DynamicDescription 这条路径在自定义附魔上会抛异常——一旦抛到
///    CardModel.get_HoverTips 里，整张卡的悬停/图鉴渲染都会失败（表现为卡片空白）。
/// 2) 所以这里直接截断附魔的提示生成，换成挂了 canonical 模型的版本：悬停可看、
///    点击可查看该附魔的索引/详情，而且永不抛异常。
/// </summary>
[HarmonyPatch(typeof(EnchantmentModel), "get_HoverTips")]
internal static class AnnotationMarkEnchantmentHoverTipPatch
{
    private static bool Prefix(
        EnchantmentModel __instance,
        ref IEnumerable<IHoverTip> __result)
    {
        if (__instance is not AnnotationMark)
        {
            return true;
        }

        __result = [AnnotationMark.InspectionTip];
        return false;
    }
}

