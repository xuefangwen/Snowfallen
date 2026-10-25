using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using SnowfallenMod.SnowfallenCode.Powers;
using SnowfallenMod.SnowfallenCode.Rewards;
using DongmingCharacter = SnowfallenMod.DongmingCode.Character.Dongming;
using SnowfallenCharacter = SnowfallenMod.SnowfallenCode.Character.Snowfallen;

namespace SnowfallenMod.SnowfallenCode.Patches;

internal static class FrostVictoryRewardState
{
    private static readonly FieldInfo CombatStateField =
        AccessTools.Field(typeof(CombatManager), "_state");

    private static readonly Dictionary<Player, int> PendingRewards = [];
    private static readonly Dictionary<Player, List<int>> PendingGoldRewards = [];

    public static void CaptureBeforeCombatCleanup(CombatManager combatManager)
    {
        CombatState combatState = CombatStateField.GetValue(combatManager) as CombatState;
        if (combatState is null ||
            combatManager.IsAboutToLose ||
            combatState.HittableEnemies.Count > 0)
        {
            return;
        }

        foreach (Player player in combatState.Players)
        {
            if (player.Creature.IsDead)
            {
                continue;
            }

            if (player.Character is SnowfallenCharacter or DongmingCharacter &&
                !PendingRewards.ContainsKey(player))
            {
                FrostPower frost = player.Creature.Powers
                    .OfType<FrostPower>()
                    .FirstOrDefault();
                int healthReward = frost is null
                    ? 0
                    : (int)Math.Floor(frost.Amount * 0.8m);

                PendingRewards[player] = Math.Max(0, healthReward);
            }

            if (!PendingGoldRewards.ContainsKey(player))
            {
                List<int> rewards = [];
                InvestorPower investor = player.Creature.Powers
                    .OfType<InvestorPower>()
                    .FirstOrDefault();
                ThunderBowlPower thunder = player.Creature.Powers
                    .OfType<ThunderBowlPower>()
                    .FirstOrDefault();
                if (investor is not null)
                {
                    rewards.AddRange(investor.GetVictoryGoldRewards());
                }

                if (thunder is not null)
                {
                    rewards.AddRange(thunder.GetVictoryGoldRewards());
                }

                PendingGoldRewards[player] = rewards;
            }
        }
    }

    public static void AddCapturedRewards(CombatRoom room)
    {
        foreach (Player player in room.CombatState.Players)
        {
            if (!PendingRewards.Remove(player, out int amount) || amount <= 0)
            {
                amount = 0;
            }

            if (amount > 0)
            {
                room.AddExtraReward(player, new FrostHealthReward(amount, player));
            }

            if (!PendingGoldRewards.Remove(player, out List<int> goldRewards))
            {
                continue;
            }

            foreach (int goldAmount in goldRewards.Where(value => value > 0))
            {
                room.AddExtraReward(
                    player,
                    new GoldReward(goldAmount, player, wasGoldStolenBack: false));
            }
        }
    }
}

[HarmonyPatch]
public static class FrostVictoryCapturePatch
{
    // Beta adds an EndCombatInternal(CombatTurnState) overload. Keep this
    // patch bound to the original parameterless cleanup method so Harmony
    // does not fail with AmbiguousMatchException during mod initialization.
    private static MethodBase TargetMethod() =>
        AccessTools.Method(
            typeof(CombatManager),
            nameof(CombatManager.EndCombatInternal),
            Type.EmptyTypes);

    [HarmonyPrefix]
    public static void Prefix(CombatManager __instance)
    {
        FrostVictoryRewardState.CaptureBeforeCombatCleanup(__instance);
    }
}

[HarmonyPatch(typeof(CombatRoom), nameof(CombatRoom.OnCombatEnded))]
public static class FrostVictoryRoomRewardPatch
{
    [HarmonyPrefix]
    public static void Prefix(CombatRoom __instance)
    {
        FrostVictoryRewardState.AddCapturedRewards(__instance);
    }
}

[HarmonyPatch(typeof(Reward), nameof(Reward.FromSerializable))]
public static class FrostHealthRewardRestorePatch
{
    [HarmonyPrefix]
    public static bool Prefix(
        SerializableReward save,
        Player player,
        ref Reward __result)
    {
        if (save.RewardType != RewardType.None || save.GoldAmount >= 0)
        {
            return true;
        }

        __result = new FrostHealthReward(-save.GoldAmount, player);
        return false;
    }
}
