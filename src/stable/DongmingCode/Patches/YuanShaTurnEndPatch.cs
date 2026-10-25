using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.DongmingCode.Mechanics;

namespace SnowfallenMod.DongmingCode.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnTurnEndInHandWrapper),
    [typeof(PlayerChoiceContext)])]
internal static class YuanShaTurnEndPatch
{
    private static readonly Func<CardModel, PlayerChoiceContext, Task>
        InvokeTurnEndEffect =
            AccessTools.MethodDelegate<Func<CardModel, PlayerChoiceContext, Task>>(
                AccessTools.Method(typeof(CardModel), "OnTurnEndInHand"));

    [HarmonyPrefix]
    private static bool Prefix(
        CardModel __instance,
        PlayerChoiceContext choiceContext,
        ref Task __result)
    {
        int extraTriggers =
            YuanShaMechanics.GetExtraTriggerCount(__instance);
        if (extraTriggers <= 0)
        {
            return true;
        }

        __result = RunWithExtraTriggers(
            __instance,
            choiceContext,
            extraTriggers);
        return false;
    }

    private static async Task RunWithExtraTriggers(
        CardModel card,
        PlayerChoiceContext choiceContext,
        int extraTriggers)
    {
        await CardPileCmd.Add(card, PileType.Play);
        if (LocalContext.IsMe(card.Owner))
        {
            await Cmd.CustomScaledWait(0.3f, 0.6f);
        }

        for (int index = 0; index <= extraTriggers; index++)
        {
            await InvokeTurnEndEffect(card, choiceContext);
        }

        if (card.Keywords.Contains(CardKeyword.Ethereal))
        {
            await CardCmd.Exhaust(
                choiceContext,
                card,
                causedByEthereal: true);
        }
        else
        {
            await CardPileCmd.Add(
                card,
                PileType.Discard.GetPile(card.Owner));
        }
    }
}
