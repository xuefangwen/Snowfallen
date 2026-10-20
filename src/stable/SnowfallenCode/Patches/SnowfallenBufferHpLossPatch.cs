using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Patches;

[HarmonyPatch(typeof(BufferPower), nameof(BufferPower.ModifyHpLostAfterOstyLate))]
internal static class SnowfallenBufferHpLossPatch
{
    private static bool Prefix(
        BufferPower __instance, Creature target, decimal amount, ValueProp props,
        Creature dealer, CardModel cardSource, ref decimal __result)
    {
        if (target != __instance.Owner ||
            !SnowfallenHpLoss.BypassesBuffer(target, props, dealer, cardSource))
            return true;

        // Returning the input leaves Buffer out of the hook's modifier list,
        // so its AfterModifyingHpLostAfterOsty callback does not consume a stack.
        __result = amount;
        return false;
    }
}
