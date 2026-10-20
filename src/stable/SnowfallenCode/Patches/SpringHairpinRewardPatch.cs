using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using SnowfallenMod.SnowfallenCode.Relics;

namespace SnowfallenMod.SnowfallenCode.Patches;

internal static class SpringHairpinRewardScope
{
    private static readonly HashSet<Reward> ActiveRewards = [];
    private static readonly Dictionary<CardModel, Reward> RewardClones = new(ReferenceEqualityComparer.Instance);
    private static readonly FieldInfo SpecialCard = AccessTools.Field(typeof(SpecialCardReward), "_card");
    internal static void Begin(Reward reward) { lock (ActiveRewards) ActiveRewards.Add(reward); }
    internal static void End(Reward reward)
    {
        lock (ActiveRewards)
        {
            ActiveRewards.Remove(reward);
            foreach (var card in RewardClones.Where(pair => ReferenceEquals(pair.Value, reward)).Select(pair => pair.Key).ToList())
                RewardClones.Remove(card);
        }
    }
    private static Reward FindReward(CardModel card) =>
        ActiveRewards.FirstOrDefault(reward => ReferenceEquals(reward.Player, card.Owner) &&
            (RewardClones.TryGetValue(card, out var origin) && ReferenceEquals(origin, reward) ||
             reward is CardReward normal && normal.Cards.Any(candidate => ReferenceEquals(candidate, card)) ||
             reward is SpecialCardReward && ReferenceEquals(SpecialCard.GetValue(reward), card)));
    internal static void TrackClone(CardModel original, CardModel clone)
    {
        lock (ActiveRewards)
        {
            Reward reward = FindReward(original);
            if (reward is not null && clone is not null) RewardClones[clone] = reward;
        }
    }
    internal static bool Contains(CardModel card)
    {
        lock (ActiveRewards)
            return FindReward(card) is not null;
    }
    internal static async Task<bool> Finish(Task<bool> task, Reward reward)
    {
        try { return await task; }
        finally { End(reward); }
    }
}

[HarmonyPatch(typeof(RunState), nameof(RunState.CloneCard))]
internal static class SpringHairpinRewardClonePatch
{
    private static void Postfix(CardModel __0, CardModel __result) =>
        SpringHairpinRewardScope.TrackClone(__0, __result);
}

[HarmonyPatch]
internal static class SpringHairpinRewardSelectionPatch
{
    private static IEnumerable<MethodBase> TargetMethods() =>
        new[] { AccessTools.Method(typeof(CardReward), "OnSelect"), AccessTools.Method(typeof(SpecialCardReward), "OnSelect") };
    private static void Prefix(Reward __instance) => SpringHairpinRewardScope.Begin(__instance);
    private static void Postfix(Reward __instance, ref Task<bool> __result) =>
        __result = SpringHairpinRewardScope.Finish(__result, __instance);
    private static Exception Finalizer(Reward __instance, Exception __exception)
    {
        if (__exception is not null) SpringHairpinRewardScope.End(__instance);
        return __exception;
    }
}

// The card reward message already carries the enchanted card. This additional
// message carries only the owning relic's consumption, so every peer saves it too.
public struct SpringHairpinConsumedMessage : INetMessage
{
    public int RelicIndex;
    public bool ShouldBroadcast => true;
    public bool ShouldBuffer => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.VeryDebug;
    public void Serialize(PacketWriter writer) => writer.WriteInt(RelicIndex);
    public void Deserialize(PacketReader reader) => RelicIndex = reader.ReadInt();
}

internal static class SpringHairpinSynchronization
{
    private sealed record Registration(INetGameService Service, MessageHandlerDelegate<SpringHairpinConsumedMessage> Handler);
    private static readonly ConditionalWeakTable<RewardSynchronizer, Registration> Registrations = new();
    internal static bool MarkConsumed(Player player, int index)
    {
        if (player is null || index < 0 || index >= player.Relics.Count || player.Relics[index] is not SpringHairpin hairpin)
            return false;
        hairpin.Available = false;
        return true;
    }
    internal static void Broadcast(SpringHairpin hairpin)
    {
        INetGameService service = RunManager.Instance.NetService;
        if (service is null || service.NetId != hairpin.Owner.NetId) return;
        int index = hairpin.Owner.Relics.ToList().FindIndex(relic => ReferenceEquals(relic, hairpin));
        if (index >= 0) service.SendMessage(new SpringHairpinConsumedMessage { RelicIndex = index });
    }
    internal static void Register(RewardSynchronizer synchronizer)
    {
        var service = (INetGameService)AccessTools.Field(typeof(RewardSynchronizer), "_gameService").GetValue(synchronizer);
        var players = (IPlayerCollection)AccessTools.Field(typeof(RewardSynchronizer), "_playerCollection").GetValue(synchronizer);
        MessageHandlerDelegate<SpringHairpinConsumedMessage> handler = (message, senderId) =>
            MarkConsumed(players.GetPlayer(senderId), message.RelicIndex);
        service.RegisterMessageHandler(handler);
        Registrations.Add(synchronizer, new Registration(service, handler));
    }
    internal static void Unregister(RewardSynchronizer synchronizer)
    {
        if (!Registrations.TryGetValue(synchronizer, out var registration)) return;
        registration.Service.UnregisterMessageHandler(registration.Handler);
        Registrations.Remove(synchronizer);
    }
}

[HarmonyPatch]
internal static class SpringHairpinSynchronizerInitPatch
{
    private static MethodBase TargetMethod() => typeof(RewardSynchronizer).GetConstructors().Single();
    private static void Postfix(RewardSynchronizer __instance) => SpringHairpinSynchronization.Register(__instance);
}

[HarmonyPatch(typeof(RewardSynchronizer), nameof(RewardSynchronizer.Dispose))]
internal static class SpringHairpinSynchronizerDisposePatch
{
    private static void Prefix(RewardSynchronizer __instance) => SpringHairpinSynchronization.Unregister(__instance);
}
