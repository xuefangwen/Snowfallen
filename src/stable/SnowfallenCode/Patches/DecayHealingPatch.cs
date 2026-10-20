using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Patches;

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal))]
public static class DecayHealingPatch
{
    public static void Prefix(Creature creature, ref decimal amount)
    {
        if (creature?.Powers.OfType<VitalityPower>().Any() == true &&
            amount > 0m)
        {
            amount *= 1.5m;
        }

        decimal stacks = creature?.Powers
            .OfType<DecayPower>()
            .FirstOrDefault()?.Amount ?? 0m;
        if (stacks <= 0m || amount <= 0m)
        {
            return;
        }

        amount *= Math.Max(0m, 1m - 0.5m * stacks);
    }
}
