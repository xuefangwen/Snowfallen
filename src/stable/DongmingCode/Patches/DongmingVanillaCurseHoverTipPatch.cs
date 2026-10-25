using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.DongmingCode.Character;
using SnowfallenMod.DongmingCode.Mechanics;

namespace SnowfallenMod.DongmingCode.Patches;

[HarmonyPatch(typeof(CardModel), "get_HoverTips")]
internal static class DongmingVanillaCurseHoverTipPatch
{
    private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        IHoverTip counterpart = PurificationMechanics.GetPurifiedHoverTip(__instance);
        if (counterpart is null ||
            !__instance.IsMutable ||
            __instance.Owner?.Character is not Dongming)
        {
            return;
        }

        __result = __result.Append(counterpart).Distinct();
    }
}
