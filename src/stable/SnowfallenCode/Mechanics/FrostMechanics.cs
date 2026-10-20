using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Mechanics;

public static class FrostMechanics
{
    public static async Task<decimal> Gain(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal requested,
        Creature applier,
        CardModel source)
    {
        decimal current = target.Powers.OfType<FrostPower>().FirstOrDefault()?.Amount ?? 0m;
        decimal maximum = Math.Max(0m, target.MaxHp);
        decimal gained = Math.Min(Math.Max(0m, requested), Math.Max(0m, maximum - current));
        if (gained > 0m)
        {
            await PowerCmd.Apply<FrostPower>(choiceContext, target, gained, applier, source);
        }

        return gained;
    }
}
