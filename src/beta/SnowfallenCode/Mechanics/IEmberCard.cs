using MegaCrit.Sts2.Core.Models;

namespace SnowfallenMod.SnowfallenCode.Mechanics;

/// <summary>
/// Marks cards that participate in Snowfallen's Ember synergies and fusion.
/// Words Flying implements this contract in addition to the generated Ember.
/// </summary>
public interface IEmberCard
{
    CardModel Card { get; }
    decimal FusionDamage { get; }
    decimal TemporaryDamageBonus { get; }
    decimal PlayBlock { get; }
    int ExtraDrawOnPlay { get; }
    bool IsAoeEffect { get; }
    bool IsAoeThisTurnEffect { get; }
    int WeakOnPlay { get; }
    int VulnerableOnPlay { get; }
    bool AppliesTwist { get; }
    bool ReturnNextTurn { get; }
    int ReplayCount { get; }
    decimal FusionGrowth { get; }
    long MergeOrder { get; }

    void AddPermanentDamageBonus(decimal amount);
    void AddPermanentBlockBonus(decimal amount);
    void AddReplayBonus(int amount);
    void AddTemporaryDamageBonus(decimal amount);
    void AddDrawOnPlay(int amount);
    void AddWeakOnExhaust(int amount = 1);
    void AddVulnerableOnPlay(int amount = 1);
    void AddTwistOnExhaust();
    void MakeAoe();
    void MakeAoEThisTurn();
    void Absorb(IEmberCard absorbed);
    void SetFusionSubject(bool isFusionSubject);
    void AssignMergeOrder(long order);
    void ClearReturnNextTurn();
}

