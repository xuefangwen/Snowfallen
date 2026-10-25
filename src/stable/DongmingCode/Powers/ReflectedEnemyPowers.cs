using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.DongmingCode.Mechanics;

namespace SnowfallenMod.DongmingCode.Powers;

// Only Amount is persistent combat state. Do not add SavedProperty fields here:
// the installed BaseLib does not serialize arbitrary PowerModel fields.
public abstract class ReflectedEnemyPower : DongmingPower
{
    protected abstract PowerModel Original { get; }
    public override LocString Title => Original.Title;
    public override string CustomPackedIconPath => Original.PackedIconPath;
    public override string CustomBigIconPath => Original.ResolvedBigIconPath;
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override Task AfterApplied(Creature applier, CardModel source) => ReflectedEnemyMechanics.Refresh(Owner);
    public override Task AfterRemoved(Creature oldOwner) => ReflectedEnemyMechanics.Refresh(oldOwner);
    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power,
        decimal amount, Creature applier, CardModel source)
    {
        if (power != this) return Task.CompletedTask;
        if (StackType == PowerStackType.Single && Amount > 1) SetAmount(1);
        return ReflectedEnemyMechanics.Refresh(Owner);
    }
}

public sealed class ReflectedRingingPower : ReflectedEnemyPower
{
    protected override PowerModel Original => ModelDb.Power<RingingPower>();
    // 1: await a normal action; 2: ready; 3: the inserted stun is pending.
    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power,
        decimal amount, Creature applier, CardModel source)
    {
        if (power == this && amount > 0) SetAmount(Amount - (int)amount == 3 ? 3 : 1);
        return Task.CompletedTask;
    }
}

public sealed class ReflectedTenderPower : ReflectedEnemyPower
{
    protected override PowerModel Original => ModelDb.Power<TenderPower>();
    public override async Task AfterDamageReceived(PlayerChoiceContext context, Creature target,
        DamageResult result, ValueProp props, Creature dealer, CardModel source)
    {
        if (target == Owner && Owner.IsAlive && result.UnblockedDamage > 0)
            await ReflectedEnemyMechanics.LoseStrength<TenderPendingLossPower>(Owner, 2);
    }
}

public sealed class ReflectedChainsPower : ReflectedEnemyPower
{
    protected override PowerModel Original => ModelDb.Power<ChainsOfBindingPower>();
    public override async Task AfterAttack(PlayerChoiceContext context, AttackCommand command)
    {
        if (!Owner.IsAlive || command.Attacker?.Side == Owner.Side ||
            (command.Attacker?.Player ?? command.Attacker?.PetOwner) == null ||
            !command.Results.SelectMany(r => r).Any(r => r.Receiver == Owner)) return;
        int round = Owner.CombatState.RoundNumber + 1;
        var consumed = Owner.GetPower<ReflectedChainsRoundPower>();
        if (consumed?.Amount == round) return;
        // Consume before the first awaited generation; one marker per monster,
        // not per attacker, hit, reflecting player, or client.
        if (consumed != null) consumed.SetAmount(round);
        else await ReflectedEnemyMechanics.Add<ReflectedChainsRoundPower>(Owner, round);
        await ReflectedEnemyMechanics.GiveSouls(Owner.CombatState);
    }
}

public sealed class ReflectedTangledPower : ReflectedEnemyPower
{
    protected override PowerModel Original => ModelDb.Power<TangledPower>();
    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner.GetPower<TangledFixedBlockPower>() is { } budget) await PowerCmd.Remove(budget);
        await base.AfterRemoved(oldOwner);
    }
    // Integer flags survive combat snapshots: 1=pending, 2=active, 3=both.
    public bool IsActive => (Amount & 2) != 0;
    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power,
        decimal amount, Creature applier, CardModel source)
    {
        if (power != this || amount <= 0) return Task.CompletedTask;
        int previous = Amount - (int)amount;
        SetAmount((previous & 2) != 0 ? 3 : 1);
        return ReflectedEnemyMechanics.Refresh(Owner);
    }
    public override async Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side || combatState != Owner.CombatState || !participants.Contains(Owner))
            return;
        if ((Amount & 1) != 0)
        {
            SetAmount(2);
            await ReflectedEnemyMechanics.FreezeTangledBlock(Owner);
        }
        await ReflectedEnemyMechanics.Refresh(Owner);
    }
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || !participants.Contains(Owner) || !IsActive) return;
        if (Owner.GetPower<TangledFixedBlockPower>() is { } budget) await PowerCmd.Remove(budget);
        if ((Amount & 1) != 0)
        {
            SetAmount(1);
            await ReflectedEnemyMechanics.Refresh(Owner);
        }
        else await PowerCmd.Remove(this);
    }
}

public sealed class ReflectedDampenPower : ReflectedEnemyPower
{
    protected override PowerModel Original => ModelDb.Power<DampenPower>();
    public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power,
        decimal amount, Creature applier, CardModel source)
    {
        if (power != this || amount <= 0 || !Owner.IsAlive) return;
        // A successful reflected application actually reduces the vanilla
        // stats, so later stat-dependent monster effects also see the loss.
        // If Artifact blocked the outer debuff, this hook is not dispatched.
        await PowerCmd.Apply(context, ModelDb.Power<StrengthPower>().ToMutable(), Owner, -5 * amount, applier, source);
        await PowerCmd.Apply(context, ModelDb.Power<DexterityPower>().ToMutable(), Owner, -5 * amount, applier, source);
    }
}

public sealed class ReflectedHexPower : ReflectedEnemyPower
{
    protected override PowerModel Original => ModelDb.Power<HexPower>();
    public override async Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!Owner.IsAlive || side != Owner.Side || combatState != Owner.CombatState ||
            !participants.Contains(Owner) || Owner.GetPower<ReflectedHexPower>() != this) return;
        int round = combatState.RoundNumber + 1;
        var consumed = Owner.GetPower<HexMaxHpRoundPower>();
        if (consumed?.Amount == round) return;
        // Mark before awaiting; duplicate application/hook dispatch cannot
        // multiply the reduction. Only this integer is persisted/synchronized.
        if (consumed != null) consumed.SetAmount(round);
        else await ReflectedEnemyMechanics.Add<HexMaxHpRoundPower>(Owner, round);
        ReflectedEnemyMechanics.ReduceHexMaxHp(Owner);
    }
}

public sealed class ReflectedSmoggyPower : ReflectedEnemyPower
{
    protected override PowerModel Original => ModelDb.Power<SmoggyPower>();
    public override Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (ReflectedEnemyMechanics.ActingMonster.Value?.Creature != Owner) return Task.CompletedTask;
        return ReflectedEnemyMechanics.AfterSpawn(Task.CompletedTask, creature, Owner.Monster);
    }
}

public abstract class ReflectedHiddenPower : DongmingPower
{
    protected override bool IsVisibleInternal => false;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

// Amount is the only saved payload. No runtime owner/summoner references.
public sealed class SmoggySummonPower : ReflectedHiddenPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override Task BeforeAttack(AttackCommand command) => command.Attacker == Owner
        ? ReflectedEnemyMechanics.BeforeSmoggySummonAttack(command) : Task.CompletedTask;
}

public sealed class ReflectedChainsRoundPower : ReflectedHiddenPower { }
public sealed class HexMaxHpRoundPower : ReflectedHiddenPower { }
// Encodes 2*(fixedBlock+1)+consumedBit. All persistent data is an integer;
// mark consumed synchronously before granting block, preserving the display.
public sealed class TangledFixedBlockPower : ReflectedHiddenPower { }

// Tender is immediate, including damage during an action; only the debt
// present at the start of an action expires at its end.
public sealed class TenderPendingLossPower : ReflectedHiddenPower { }
public sealed class ReflectedActiveLossPower : ReflectedHiddenPower { }
// Retain the old model ID so existing pending records can be discarded
// without applying the retired temporary Strength/attack-preview effect.
public sealed class HexPendingLossPower : ReflectedHiddenPower
{
    public override Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState) =>
        side == Owner.Side && combatState == Owner.CombatState && participants.Contains(Owner)
            ? PowerCmd.Remove(this) : Task.CompletedTask;
}
