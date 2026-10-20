using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using SnowfallenMod.SnowfallenCode.Powers;
namespace SnowfallenMod.SnowfallenCode.Mechanics;

public static class ApricotBlossomRainEffect
{
    public static async Task Resolve(PlayerChoiceContext context, Player owner, int turn,
        decimal healingPerStack, decimal stacks)
    {
        if (turn <= 0 || stacks <= 0 || owner.Creature.IsDead) return;
        if ((turn & 1) != 0)
            await PowerCmd.Apply<VitalityPower>(context, owner.Creature, 2m * stacks, owner.Creature, null);
        else
            await CreatureCmd.Heal(owner.Creature, healingPerStack * stacks);
    }
}
