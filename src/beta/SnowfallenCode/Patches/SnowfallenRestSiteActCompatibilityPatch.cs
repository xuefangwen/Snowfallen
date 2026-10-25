using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using SnowfallenCharacter = SnowfallenMod.SnowfallenCode.Character.Snowfallen;
using DongmingCharacter = SnowfallenMod.DongmingCode.Character.Dongming;

namespace SnowfallenMod.SnowfallenCode.Patches;

// The vanilla rest-site initializer only has animations for acts 0, 1 and 2.
// Keep the entire initializer (including its UI signal connections) running
// before the full edition's custom visuals replace the placeholder visuals.
[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))]
public static class SnowfallenRestSiteActCompatibilityPatch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo resolve = AccessTools.Method(
            typeof(SnowfallenRestSiteActCompatibilityPatch), nameof(ResolveAnimationActIndex));
        int matches = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            yield return instruction;
            if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
                instruction.operand is MethodInfo getter &&
                getter.Name == "get_CurrentActIndex" && getter.ReturnType == typeof(int))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call, resolve);
                matches++;
            }
        }

        if (matches != 1)
        {
            throw new InvalidOperationException(
                $"Snowfallen rest-site compatibility expected one act-index read, found {matches}.");
        }
    }

    private static int ResolveAnimationActIndex(int actIndex, NRestSiteCharacter character)
    {
        // This changes only the value consumed by the animation switch, never
        // RunState.CurrentActIndex or other characters' rest-site behavior.
        return actIndex >= 3 && character.Player?.Character is
            (SnowfallenCharacter or DongmingCharacter)
            ? 2
            : actIndex;
    }
}
