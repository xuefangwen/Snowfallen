using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using SnowfallenCharacter = SnowfallenMod.SnowfallenCode.Character.Snowfallen;

namespace SnowfallenMod.SnowfallenCode.Patches;

internal static class SnowfallenAncientDialogues
{
    public static void Install(AncientDialogueSet dialogueSet, params int[] lineCounts)
    {
        string characterEntry = ModelDb.Character<SnowfallenCharacter>().Id.Entry;
        AncientDialogue[] dialogues = lineCounts
            .Select((lineCount, index) => new AncientDialogue(new string[lineCount])
            {
                VisitIndex = index == 0 ? 0 : null,
                IsRepeating = true
            })
            .ToArray();

        dialogueSet.CharacterDialogues[characterEntry] = dialogues;
    }
}

[HarmonyPatch(typeof(Darv), "DefineDialogues")]
public static class DarvDialoguePatch
{
    [HarmonyPostfix]
    public static void Postfix(ref AncientDialogueSet __result) =>
        SnowfallenAncientDialogues.Install(__result, 4);
}

[HarmonyPatch(typeof(Nonupeipe), "DefineDialogues")]
public static class NonupeipeDialoguePatch
{
    [HarmonyPostfix]
    public static void Postfix(ref AncientDialogueSet __result) =>
        SnowfallenAncientDialogues.Install(__result, 2, 5, 3);
}

[HarmonyPatch(typeof(Orobas), "DefineDialogues")]
public static class OrobasDialoguePatch
{
    [HarmonyPostfix]
    public static void Postfix(ref AncientDialogueSet __result) =>
        SnowfallenAncientDialogues.Install(__result, 3, 3);
}

[HarmonyPatch(typeof(Pael), "DefineDialogues")]
public static class PaelDialoguePatch
{
    [HarmonyPostfix]
    public static void Postfix(ref AncientDialogueSet __result) =>
        SnowfallenAncientDialogues.Install(__result, 3, 3);
}

[HarmonyPatch(typeof(Tanx), "DefineDialogues")]
public static class TanxDialoguePatch
{
    [HarmonyPostfix]
    public static void Postfix(ref AncientDialogueSet __result) =>
        SnowfallenAncientDialogues.Install(__result, 6, 5);
}

[HarmonyPatch(typeof(Tezcatara), "DefineDialogues")]
public static class TezcataraDialoguePatch
{
    [HarmonyPostfix]
    public static void Postfix(ref AncientDialogueSet __result) =>
        SnowfallenAncientDialogues.Install(__result, 3, 4);
}

[HarmonyPatch(typeof(Vakuu), "DefineDialogues")]
public static class VakuuDialoguePatch
{
    [HarmonyPostfix]
    public static void Postfix(ref AncientDialogueSet __result) =>
        SnowfallenAncientDialogues.Install(__result, 5, 4);
}

[HarmonyPatch(typeof(TheArchitect), "DefineDialogues")]
public static class TheArchitectDialoguePatch
{
    [HarmonyPostfix]
    public static void Postfix(ref AncientDialogueSet __result) =>
        SnowfallenAncientDialogues.Install(__result, 3, 5, 4, 5);
}

