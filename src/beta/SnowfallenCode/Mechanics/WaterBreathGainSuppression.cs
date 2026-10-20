using MegaCrit.Sts2.Core.Entities.Creatures;

namespace SnowfallenMod.SnowfallenCode.Mechanics;

internal static class WaterBreathGainSuppression
{
    private static readonly Dictionary<Creature, int> SuppressionDepth = [];

    public static bool IsSuppressed(Creature creature)
    {
        return SuppressionDepth.TryGetValue(creature, out int depth) && depth > 0;
    }

    public static async Task Run(Creature creature, Func<Task> action)
    {
        SuppressionDepth.TryGetValue(creature, out int depth);
        SuppressionDepth[creature] = depth + 1;

        try
        {
            await action();
        }
        finally
        {
            if (depth == 0)
            {
                SuppressionDepth.Remove(creature);
            }
            else
            {
                SuppressionDepth[creature] = depth;
            }
        }
    }
}

