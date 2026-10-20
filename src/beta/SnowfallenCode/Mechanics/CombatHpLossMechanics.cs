using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using SnowfallenMod.SnowfallenCode.Powers;
namespace SnowfallenMod.SnowfallenCode.Mechanics;

public static class CombatHpLossMechanics
{
    public static int GetLoss(Player player) => player?.Creature.Powers
        .OfType<CombatHpLossPower>().FirstOrDefault()?.LostHp ?? 0;
    public static async Task Ensure(Player player)
    {
        if (player.Creature.Powers.OfType<CombatHpLossPower>().Any()) return;
        await PowerCmd.Apply<CombatHpLossPower>(new BlockingPlayerChoiceContext(),
            player.Creature, 1m, player.Creature, null);
    }
}
