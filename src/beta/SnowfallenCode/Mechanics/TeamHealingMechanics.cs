using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Mechanics;

public static class TeamHealingMechanics
{
    public static async Task<TeamHealingTrackerPower> EnsureTracker(
        PlayerChoiceContext choiceContext,
        Player owner,
        CardModel source)
    {
        TeamHealingTrackerPower tracker = owner.Creature.Powers
            .OfType<TeamHealingTrackerPower>()
            .FirstOrDefault();
        if (tracker is not null)
        {
            return tracker;
        }

        await PowerCmd.Apply<TeamHealingTrackerPower>(
            choiceContext,
            owner.Creature,
            1m,
            owner.Creature,
            source);

        return owner.Creature.Powers
            .OfType<TeamHealingTrackerPower>()
            .First();
    }
}

