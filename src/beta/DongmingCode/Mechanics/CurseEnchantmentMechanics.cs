using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using SnowfallenMod.DongmingCode.Enchantments;
using SnowfallenMod.DongmingCode.Powers;

namespace SnowfallenMod.DongmingCode.Mechanics;

public static class CurseEnchantmentMechanics
{
    private static readonly HashSet<CardModel> ExhaustingFromPlay =
        new(ReferenceEqualityComparer.Instance);
    private static readonly HashSet<CardModel> ResolvingCardPlays =
        new(ReferenceEqualityComparer.Instance);

    public static void BeginCardPlay(CardModel card)
    {
        if (card.Enchantment is DongmingCurseEnchantment)
        {
            ResolvingCardPlays.Add(card);
        }
    }

    public static async Task CompleteCardPlay(
        Task cardPlayTask,
        CardModel card)
    {
        try
        {
            await cardPlayTask;
        }
        finally
        {
            ResolvingCardPlays.Remove(card);
        }
    }

    public static void MarkExhaustOrigin(CardModel card)
    {
        if (card.Enchantment is DongmingCurseEnchantment &&
            ResolvingCardPlays.Contains(card))
        {
            ExhaustingFromPlay.Add(card);
        }
    }

    public static bool ConsumeExhaustedFromPlay(CardModel card) =>
        ExhaustingFromPlay.Remove(card);

    public static async Task Trigger(
        PlayerChoiceContext choiceContext,
        CardModel card)
    {
        await PowerCmd.Apply<DoomPower>(
            choiceContext,
            card.Owner.Creature,
            3m,
            card.Owner.Creature,
            card);
        await MoonlightMechanics.Gain(card.Owner, 1m);
    }
}

