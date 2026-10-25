using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Monsters;
using SnowfallenMod.DongmingCode.Cards;

namespace SnowfallenMod.DongmingCode.Patches;

[HarmonyPatch(typeof(Aeonglass), "set_WitherUpgradeCount")]
internal static class PurifiedWitherGrowthPatch
{
    [HarmonyPrefix]
    private static void Prefix(Aeonglass __instance, out int __state) =>
        __state = __instance.WitherUpgradeCount;

    [HarmonyPostfix]
    private static void Postfix(Aeonglass __instance, int __state)
    {
        int gained = __instance.WitherUpgradeCount - __state;
        if (gained <= 0 || __instance.CombatState is null) return;
        foreach (var player in __instance.CombatState.Players)
        foreach (PurifiedWither card in player.PlayerCombatState.AllCards.OfType<PurifiedWither>())
            card.HealingAmount += gained * 3;
    }
}
