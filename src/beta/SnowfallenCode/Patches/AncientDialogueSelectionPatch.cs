using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;
using SnowfallenCharacter = SnowfallenMod.SnowfallenCode.Character.Snowfallen;

namespace SnowfallenMod.SnowfallenCode.Patches;

[HarmonyPatch(typeof(AncientDialogueSet), nameof(AncientDialogueSet.GetValidDialogues))]
public static class AncientDialogueSelectionPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        AncientDialogueSet __instance,
        ModelId characterId,
        ref IEnumerable<AncientDialogue> __result)
    {
        CharacterModel snowfallen = ModelDb.Character<SnowfallenCharacter>();
        if (characterId != snowfallen.Id)
        {
            return;
        }

        if (!__instance.CharacterDialogues.TryGetValue(
                snowfallen.Id.Entry,
                out IReadOnlyList<AncientDialogue> snowfallenDialogues))
        {
            return;
        }

        HashSet<AncientDialogue> allowed = new(snowfallenDialogues);
        __result = __result.Where(allowed.Contains).ToArray();
    }
}

