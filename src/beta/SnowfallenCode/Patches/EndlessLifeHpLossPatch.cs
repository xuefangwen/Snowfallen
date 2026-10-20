using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Patches;

// Record committed HP changes before HP-change hooks can heal the creature.
// Both game versions commit HP through this setter, including damage and costs.
[HarmonyPatch(typeof(Creature), nameof(Creature.CurrentHp), MethodType.Setter)]
public static class EndlessLifeHpLossPatch
{
    private static void Prefix(Creature __instance, out int __state) =>
        __state = __instance.CurrentHp;

    private static void Postfix(Creature __instance, int __state)
    {
        if (!VitalityPower.IsRestoringMaxHp(__instance))
            foreach (var tracker in __instance.Powers.OfType<CombatHpLossPower>())
                tracker.Record(__state, __instance.CurrentHp);
        foreach (var power in __instance.Powers.OfType<EndlessLifePowerBase>())
        {
            if (VitalityPower.IsRestoringMaxHp(__instance))
                power.SynchronizeHpWithoutLoss();
            else
                power.RecordCommittedHpChange(__state, __instance.CurrentHp);
        }
    }
}
