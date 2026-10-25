using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using SnowfallenMod.SnowfallenCode.Animation;

namespace SnowfallenMod.SnowfallenCode.Patches;

// Restore persistent form art when loading an existing combat save. AfterApplied
// is not called again for powers already present on a deserialized creature.
[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class MysticFoxFormVfxReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance) => MysticFoxFormVfx.Ensure(__instance);
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
public static class MysticFoxFormVfxDeathPatch
{
    [HarmonyPrefix]
    public static void Prefix(NCreature __instance) => MysticFoxFormVfx.Remove(__instance);
}
