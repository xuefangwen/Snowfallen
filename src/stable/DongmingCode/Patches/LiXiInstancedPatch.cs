using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.DongmingCode.Powers;

namespace SnowfallenMod.DongmingCode.Patches;

/// <summary>
/// PowerCmd.Apply 里靠 ModelDb.Power&lt;T&gt;().InstanceType 决定
/// "新建一条独立实例"还是"合并进已有那条"（原版"神气致胜"这类多栏位 buff 就是这么做的）。
/// LiXiPower 继承自 BaseLib 的 CustomPowerModel，在 C# 层无法直接 override 该属性，
/// 因此这里对 getter 做后缀补丁：只要是 LiXiPower，就告诉引擎它是 instanced，
/// 于是每张"利息"牌各自占一条 buff、独立计数。
/// </summary>
[HarmonyPatch(typeof(PowerModel), "get_InstanceType")]
internal static class LiXiInstancedPatch
{
    [HarmonyPostfix]
    private static void Postfix(
        PowerModel __instance,
        ref PowerInstanceType __result)
    {
        if (__instance is LiXiPower)
        {
            __result = PowerInstanceType.Instanced;
        }
    }
}
