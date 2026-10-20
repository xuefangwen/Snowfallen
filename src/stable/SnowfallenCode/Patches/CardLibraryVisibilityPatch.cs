using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Cards;

namespace SnowfallenMod.SnowfallenCode.Patches;

[HarmonyPatch(typeof(CardModel), "get_ShouldShowInCardLibrary")]
internal static class CardLibraryVisibilityPatch
{
    [HarmonyPostfix]
    private static void HideLegacyPrologue(
        CardModel __instance,
        ref bool __result)
    {
        if (__instance is Prologue)
        {
            __result = false;
        }
    }
}
