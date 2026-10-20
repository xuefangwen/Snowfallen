using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using SnowfallenCharacter = SnowfallenMod.SnowfallenCode.Character.Snowfallen;

namespace SnowfallenMod.SnowfallenCode.Patches;

[HarmonyPatch(typeof(Neow), "DefineDialogues")]
public static class NeowDialoguePatch
{
    private const string WelcomeSfx = "event:/sfx/npcs/neow/neow_welcome";
    private const string CuriousSfx = "event:/sfx/npcs/neow/neow_curious";
    private const string SleepySfx = "event:/sfx/npcs/neow/neow_sleepy";

    [HarmonyPostfix]
    public static void Postfix(ref AncientDialogueSet __result)
    {
        string characterEntry = ModelDb.Character<SnowfallenCharacter>().Id.Entry;

        __result.CharacterDialogues[characterEntry] =
        [
            new AncientDialogue(
            [
                WelcomeSfx,
                string.Empty,
                CuriousSfx,
                string.Empty,
                SleepySfx
            ])
            {
                VisitIndex = 0,
                IsRepeating = true
            },
            new AncientDialogue(
            [
                CuriousSfx,
                string.Empty,
                SleepySfx,
                string.Empty
            ])
            {
                IsRepeating = true
            }
        ];
    }
}
