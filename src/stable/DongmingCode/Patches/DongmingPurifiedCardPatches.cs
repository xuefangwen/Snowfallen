using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.DongmingCode.Mechanics;

namespace SnowfallenMod.DongmingCode.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
internal static class DongmingCurseEnchantmentCardPlayPatch
{
    [HarmonyPrefix]
    private static void Prefix(CardModel __instance)
    {
        CurseEnchantmentMechanics.BeginCardPlay(__instance);
    }

    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref Task __result)
    {
        __result = CurseEnchantmentMechanics.CompleteCardPlay(
            __result,
            __instance);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.SpendResources))]
internal static class DongmingCanXingResourceSpendPatch
{
    [HarmonyPrefix]
    private static void Prefix(CardModel __instance)
    {
        CanXingMechanics.BeginResourceSpend(__instance);
    }

    [HarmonyPostfix]
    private static void Postfix(
        CardModel __instance,
        ref Task<(int Energy, int Stars)> __result)
    {
        __result = CanXingMechanics.CompleteResourceSpend(
            __result,
            __instance);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
internal static class DongmingCanXingCardPlayPatch
{
    [HarmonyPostfix]
    private static void Postfix(
        CardModel __instance,
        PlayerChoiceContext choiceContext,
        ref Task __result)
    {
        __result = CanXingMechanics.CompleteCardPlay(
            __result,
            __instance,
            choiceContext);
    }
}

[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Exhaust),
    [typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool), typeof(bool)])]
internal static class DongmingPurifiedExhaustOriginPatch
{
    [HarmonyPrefix]
    private static void Prefix(CardModel card)
    {
        PurificationMechanics.MarkExhaustingFromHand(card);
        CurseEnchantmentMechanics.MarkExhaustOrigin(card);
    }
}
