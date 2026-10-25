using HarmonyLib;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SnowfallenMod.SnowfallenCode.Patches;

/// <summary>
/// The Necrobinder placeholder creates an Osty pet whose nameplate can
/// overlap summoned enemies (for example, the Old Day Shadow), displaying
/// the internal "Snowfallen" label below the enemy's name. Keep the pet and
/// its health/power UI intact, but suppress the Osty nameplate itself.
/// </summary>
[HarmonyPatch(typeof(NCreatureStateDisplay), nameof(NCreatureStateDisplay.SetCreature))]
internal static class DongmingOstyNameplateSetCreaturePatch
{
    private static readonly FieldInfo CreatureField =
        AccessTools.Field(typeof(NCreatureStateDisplay), "_creature");

    [HarmonyPostfix]
    private static void Postfix(NCreatureStateDisplay __instance)
    {
        if (IsDongmingOsty(__instance))
        {
            __instance.HideNameplate();
        }
    }

    internal static bool IsDongmingOsty(NCreatureStateDisplay display)
    {
        Creature creature = CreatureField?.GetValue(display) as Creature;
        if (creature is null)
        {
            return false;
        }

        // Match Osty by model rather than by owner.  The placeholder can be
        // owned by either custom character, and the owner-dependent check
        // previously missed the instance shown below Old Day Shadow.
        return creature.Monster is Osty;
    }
}

[HarmonyPatch(typeof(NCreatureStateDisplay), nameof(NCreatureStateDisplay.ShowNameplate))]
internal static class DongmingOstyNameplateShowPatch
{
    [HarmonyPrefix]
    private static bool Prefix(NCreatureStateDisplay __instance)
    {
        // Prevent hover/focus from revealing the suppressed placeholder pet
        // nameplate again after SetCreature hid it.
        return !DongmingOstyNameplateSetCreaturePatch.IsDongmingOsty(__instance);
    }
}

