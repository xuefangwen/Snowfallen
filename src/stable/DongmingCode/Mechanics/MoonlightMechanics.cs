using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.DongmingCode.Enchantments;

namespace SnowfallenMod.DongmingCode.Mechanics;

public static class MoonlightMechanics
{
    public static int Amount(Player player) =>
        player.PlayerCombatState?.Stars ?? 0;

    public static Task Gain(Player player, decimal amount)
    {
        return amount <= 0m
            ? Task.CompletedTask
            : PlayerCmd.GainStars(amount, player);
    }

    public static Task Spend(Player player, decimal amount)
    {
        return amount <= 0m
            ? Task.CompletedTask
            : PlayerCmd.LoseStars(amount, player);
    }

    public static int CountCursedCards(Player player) =>
        player.Deck.Cards.Count(IsCursedCard);

    public static bool IsCursedCard(CardModel card) =>
        card.Type == CardType.Curse ||
        card.Enchantment is DongmingCurseEnchantment;
}
