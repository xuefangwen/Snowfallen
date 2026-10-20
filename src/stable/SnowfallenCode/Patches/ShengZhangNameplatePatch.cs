#nullable enable

using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using SnowfallenMod.SnowfallenCode.Monsters;

namespace SnowfallenMod.SnowfallenCode.Patches;

[HarmonyPatch(
    typeof(NCreatureStateDisplay),
    nameof(NCreatureStateDisplay.SetCreature))]
internal static class ShengZhangNameplateSetCreaturePatch
{
    private static void Postfix(NCreatureStateDisplay __instance, Creature creature)
    {
        if (creature.Monster is ShengZhangUnit)
        {
            SetNameplate(__instance);
        }
    }

    internal static void SetNameplate(NCreatureStateDisplay stateDisplay)
    {
        MegaLabel? label = stateDisplay.GetNodeOrNull<MegaLabel>("%NameplateLabel");
        label?.SetTextAutoSize("生长子单位");
    }
}

[HarmonyPatch(
    typeof(NCreatureStateDisplay),
    nameof(NCreatureStateDisplay.ShowNameplate))]
internal static class ShengZhangNameplateShowPatch
{
    private static void Postfix(NCreatureStateDisplay __instance)
    {
        Creature? creature = Traverse.Create(__instance)
            .Field("_creature")
            .GetValue<Creature>();
        if (creature?.Monster is ShengZhangUnit)
        {
            ShengZhangNameplateSetCreaturePatch.SetNameplate(__instance);
        }
    }
}

[HarmonyPatch(typeof(NCreatureStateDisplay), "RefreshValues")]
internal static class ShengZhangNameplateRefreshPatch
{
    private static void Postfix(NCreatureStateDisplay __instance)
    {
        Creature? creature = Traverse.Create(__instance)
            .Field("_creature")
            .GetValue<Creature>();
        if (creature?.Monster is ShengZhangUnit)
        {
            ShengZhangNameplateSetCreaturePatch.SetNameplate(__instance);
        }
    }
}
