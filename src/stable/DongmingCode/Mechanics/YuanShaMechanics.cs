using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.DongmingCode.Powers;

namespace SnowfallenMod.DongmingCode.Mechanics;

public static class YuanShaMechanics
{
    public static int GetExtraTriggerCount(CardModel card)
    {
        if (card.Owner?.Creature is null ||
            card.Type == CardType.Status ||
            !MoonlightMechanics.IsCursedCard(card))
        {
            return 0;
        }

        return Math.Max(
            0,
            card.Owner.Creature.GetPower<YuanShaPower>()?.Amount ?? 0);
    }
}
