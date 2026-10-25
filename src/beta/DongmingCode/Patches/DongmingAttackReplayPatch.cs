using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.DongmingCode.Cards;

namespace SnowfallenMod.DongmingCode.Patches;

[HarmonyPatch(typeof(CardModel), "get_BaseReplayCount")]
public static class DongmingAttackReplayPatch
{
    public static void Postfix(CardModel __instance, ref int __result)
    {
        if (__instance is ZhanE zhanE &&
            zhanE.IsMutable &&
            zhanE.Owner?.PlayerCombatState is not null)
        {
            __result += zhanE.CurseReplayCount;
        }
    }
}

// Patch the method used by GeneratePlayCount directly. The trivial
// BaseReplayCount getter can already be inlined when another mod patches
// GetEnchantedReplayCount before Snowfallen, bypassing the getter postfix.
[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetEnchantedReplayCount))]
public static class YuanJiaCurseReplayPatch
{
    public static void Postfix(CardModel __instance, ref int __result)
    {
        if (__instance is YuanJia yuanJia &&
            yuanJia.IsMutable &&
            yuanJia.Owner?.PlayerCombatState is not null)
        {
            __result += yuanJia.CurseReplayCount;
        }
    }
}

