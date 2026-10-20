using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using SnowfallenMod.SnowfallenCode.Powers;
using SnowfallenMod.SnowfallenCode.Rewards;
using SnowfallenCharacter = SnowfallenMod.SnowfallenCode.Character.Snowfallen;

namespace SnowfallenMod.SnowfallenCode.Patches;

internal static class FrostVictoryRewardState
{
    private static readonly Dictionary<Player, int> PendingRewards = [];
    private static readonly Dictionary<Player, List<int>> PendingGoldRewards = [];

    public static void CaptureBeforeCombatCleanup(
        CombatState combatState,
        bool isAboutToLose)
    {
        if (combatState is null ||
            isAboutToLose ||
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

            if (player.Character is SnowfallenCharacter &&
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
    // Normal Beta victories call this overload directly, bypassing the
    // parameterless wrapper. CombatTurnState is internal to sts2, so resolve
    // its signature and properties without exposing that type in mod code.
    private static readonly Type TurnStateType = typeof(CombatManager).Assembly
        .GetType("MegaCrit.Sts2.Core.Combat.CombatTurnState", throwOnError: true);

    private static readonly PropertyInfo StateProperty =
        AccessTools.Property(TurnStateType, "State")
        ?? throw new MissingMemberException(TurnStateType.FullName, "State");

    private static readonly PropertyInfo PendingLossProperty =
        AccessTools.Property(TurnStateType, "PendingLoss")
        ?? throw new MissingMemberException(TurnStateType.FullName, "PendingLoss");

    private static readonly PropertyInfo IsLiveProperty =
        AccessTools.Property(TurnStateType, "IsLive")
        ?? throw new MissingMemberException(TurnStateType.FullName, "IsLive");

    private static MethodBase TargetMethod() =>
        AccessTools.Method(
            typeof(CombatManager),
            "EndCombatInternal",
            [TurnStateType])
        ?? throw new MissingMethodException(
            typeof(CombatManager).FullName,
            "EndCombatInternal(CombatTurnState)");

    [HarmonyPrefix]
    public static void Prefix(object __0)
    {
        // Use the argument's combat, never the manager's current combat:
        // a stale continuation must not capture rewards from a later room.
        // This also prevents a second end call from capturing rewards again.
        if (__0 is null || IsLiveProperty.GetValue(__0) is not true)
        {
            return;
        }

        FrostVictoryRewardState.CaptureBeforeCombatCleanup(
            StateProperty.GetValue(__0) as CombatState,
            PendingLossProperty.GetValue(__0) is not null);
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

