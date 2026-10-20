#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Monsters;

namespace SnowfallenMod.SnowfallenCode.Patches;

[HarmonyPatch(
    typeof(PlatingPower),
    "BeforeSideTurnEndEarly")]
internal static class ShengZhangPlatingTurnEndPatch
{
    private static bool Prefix(
        PlatingPower __instance,
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants,
        ref Task __result)
    {
        if (__instance.Owner.Monster is not ShengZhangUnit)
        {
            return true;
        }

        __result = GrantUnitPlatingBlock(__instance, side);
        return false;
    }

    private static async Task GrantUnitPlatingBlock(
        PlatingPower power,
        CombatSide side)
    {
        if (side != power.Owner.Side)
        {
            return;
        }

        await CreatureCmd.GainBlock(
            power.Owner,
            power.Amount,
            ValueProp.Unpowered,
            null);
    }
}

