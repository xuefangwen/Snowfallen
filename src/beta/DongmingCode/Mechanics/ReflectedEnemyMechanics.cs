using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.DongmingCode.Powers;

namespace SnowfallenMod.DongmingCode.Mechanics;

public static class ReflectedEnemyMechanics
{
    public static Task Refresh(Creature owner) => owner?.GetCreatureNode()?.RefreshIntents() ?? Task.CompletedTask;
    public static void Trace(string text) => Log.Info("[Dongming.Reflect] " + text);
    public static bool IsTangledActive(Creature owner) => owner?.GetPower<ReflectedTangledPower>()?.IsActive == true;
    public static readonly AsyncLocal<MonsterModel> ActingMonster = new();
    internal sealed class SmoggyAttackerDiedException : Exception { }
    private static readonly ConditionalWeakTable<MoveState, Creature> MoveOwners = new();
    private static readonly FieldInfo MonsterCreatureField = AccessTools.Field(typeof(MonsterModel), "_creature");
    private static readonly FieldInfo RawIntentsField = AccessTools.Field(typeof(MoveState), "<Intents>k__BackingField");

    public static int RawBlock(Creature owner)
    {
        if (owner?.Monster?.NextMove is not { } move) return 0;
        var intents = (IReadOnlyList<AbstractIntent>)RawIntentsField.GetValue(move);
        decimal raw = intents.OfType<AttackIntent>().Sum(i => Math.Max(0, i.DamageCalc?.Invoke() ?? 0) * Math.Max(1, i.Repeats));
        return (int)Math.Min(499999998m, Math.Floor(raw / 2m));
    }

    public static async Task FreezeTangledBlock(Creature owner)
    {
        int fixedBlock = RawBlock(owner);
        int encoded = 2 * (fixedBlock + 1);
        if (owner.GetPower<TangledFixedBlockPower>() is { } existing) existing.SetAmount(encoded);
        else await Add<TangledFixedBlockPower>(owner, encoded);
        Trace($"Tangled enemy-start fixed-block={fixedBlock} move={owner.Monster?.NextMove.Id}");
    }

    public static int TangledBlock(Creature owner) => owner.GetPower<TangledFixedBlockPower>() is { } saved
        ? saved.Amount / 2 - 1 : RawBlock(owner);

    public static int? ConsumeTangledBlock(Creature owner)
    {
        if (owner.GetPower<TangledFixedBlockPower>() is not { } saved || (saved.Amount & 1) != 0) return null;
        int block = saved.Amount / 2 - 1;
        saved.SetAmount(saved.Amount + 1);
        return block;
    }

    public static PowerModel Replacement(PowerModel original) => original switch
    {
        RingingPower => ModelDb.Power<ReflectedRingingPower>(),
        TenderPower => ModelDb.Power<ReflectedTenderPower>(),
        ChainsOfBindingPower => ModelDb.Power<ReflectedChainsPower>(),
        TangledPower => ModelDb.Power<ReflectedTangledPower>(),
        DampenPower => ModelDb.Power<ReflectedDampenPower>(),
        HexPower => ModelDb.Power<ReflectedHexPower>(),
        SmoggyPower => ModelDb.Power<ReflectedSmoggyPower>(),
        _ => null
    };

    public static async Task Add<T>(Creature owner, int amount) where T : PowerModel, new()
    {
        if (amount > 0 && owner.IsAlive)
        {
            await PowerCmd.Apply(new BlockingPlayerChoiceContext(), ModelDb.Power<T>().ToMutable(), owner, amount, owner, null);
            await Refresh(owner);
        }
    }

    public static async Task LoseStrength<T>(Creature owner, int amount) where T : PowerModel, new()
    {
        if (amount <= 0 || !owner.IsAlive) return;
        int before = owner.GetPowerAmount<StrengthPower>();
        await PowerCmd.Apply(new BlockingPlayerChoiceContext(), ModelDb.Power<StrengthPower>().ToMutable(), owner, -amount, owner, null);
        // An enemy Artifact may block the actual Strength loss. Restore only
        // the amount really removed, rather than granting free Strength later.
        await Add<T>(owner, Math.Max(0, before - owner.GetPowerAmount<StrengthPower>()));
    }

    public static async Task GiveSouls(ICombatState combat)
    {
        // Stable network identity, never LocalContext or local seat order.
        foreach (var player in combat.Players.OrderBy(p => p.NetId))
        {
            if (!player.Creature.IsAlive) continue;
            Soul soul = combat.CreateCard<Soul>(player);
            CardCmd.Upgrade(soul);
            await CardPileCmd.AddGeneratedCardToCombat(soul, PileType.Hand, player);
        }
    }

    public static async Task<AttackCommand> ConvertAttack(AttackCommand command, PlayerChoiceContext context)
    {
        if (command.Attacker is not { IsAlive: true } dealer || dealer.CombatState == null ||
            CombatManager.Instance.IsOverOrEnding && dealer.CombatState.IsLiveCombat()) return command;
        await Hook.BeforeAttack(dealer.CombatState, command);
        int? amount = ConsumeTangledBlock(dealer);
        // No Move flag: the converted amount is final and must not receive
        // Dexterity/Frail a second time or multiply by the target count.
        if (amount.HasValue)
        {
            await CreatureCmd.GainBlock(dealer, amount.Value, ValueProp.Unpowered, null);
            Trace($"Tangled granted fixed-block={amount.Value}");
        }
        await Hook.AfterAttack(dealer.CombatState, context ?? new BlockingPlayerChoiceContext(), command);
        return command;
    }

    private static readonly PropertyInfo MaxHpProperty = AccessTools.Property(typeof(Creature), nameof(Creature.MaxHp));

    public static void ReduceHexMaxHp(Creature owner)
    {
        // SetMaxHpInternal/CreatureCmd.SetMaxHp also clamp current HP. The
        // property setter raises MaxHpChanged without changing current HP.
        int before = owner.MaxHp;
        int after = Math.Max(1, before - before / 5);
        MaxHpProperty.SetValue(owner, after);
        Trace($"Hex enemy-start max-hp={before}->{after} current-hp={owner.CurrentHp}");
    }

    public static async Task BeginAction(Creature owner)
    {
        int amount = owner.GetPowerAmount<TenderPendingLossPower>();
        // Transfer debt in a deterministic command sequence on every peer.
        if (amount > 0) await Add<ReflectedActiveLossPower>(owner, amount);
        if (owner.GetPower<TenderPendingLossPower>() is { } tender) await PowerCmd.Remove(tender);
    }

    public static async Task EndAction(Creature owner, bool wasStun)
    {
        if (owner.GetPower<ReflectedActiveLossPower>() is { } loss)
        {
            int restore = loss.Amount;
            await PowerCmd.Remove(loss);
            if (owner.IsAlive) await PowerCmd.Apply(new BlockingPlayerChoiceContext(),
                ModelDb.Power<StrengthPower>().ToMutable(), owner, restore, owner, null);
        }
        if (owner.GetPower<ReflectedRingingPower>() is not { } ringing || !owner.IsAlive) return;
        if (wasStun && ringing.Amount == 3) await PowerCmd.Remove(ringing);
        else if (!wasStun && ringing.Amount == 1) ringing.SetAmount(2);
    }

    public static void ScheduleStun(MonsterModel monster)
    {
        if (monster.Creature is not { IsAlive: true } owner ||
            owner.GetPower<ReflectedRingingPower>() is not { Amount: 2 } ringing ||
            !monster.NextMove.CanTransitionAway) return;
        string resume = monster.NextMove.Id;
        owner.StunInternal(_ => Task.CompletedTask, resume);
        if (monster.NextMove.Id == "STUNNED") ringing.SetAmount(3);
    }

    public static async Task AfterSpawn(Task task, Creature spawned, MonsterModel summoner)
    {
        await task;
        if (summoner?.Creature is not { IsAlive: true } owner ||
            !owner.HasPower<ReflectedSmoggyPower>() || spawned == owner ||
            spawned.Side != owner.Side || spawned.CombatState != owner.CombatState || !spawned.IsAlive ||
            !owner.CombatState.ContainsCreature(spawned)) return;
        // Entry only attaches the marker. Never damage or interrupt Add():
        // the original summon setup and the rest of the summoner's move finish.
        if (spawned.HasPower<SmoggySummonPower>()) return;
        await Add<SmoggySummonPower>(spawned, 1);
        Trace($"Smoggy marked summon={spawned.Monster?.Id.Entry} max-hp={spawned.MaxHp}");
    }

    public static async Task BeforeSmoggySummonAttack(AttackCommand command)
    {
        if (command.Attacker is not { IsAlive: true } attacker ||
            !attacker.HasPower<SmoggySummonPower>() || attacker.CombatState is not { } combat ||
            CombatManager.Instance.IsOverOrEnding && combat.IsLiveCombat()) return;
        // One command for the entire enemy snapshot: damage/deaths finish even
        // when this attacker's own lethal hit is the first in the target list.
        Creature[] targets = combat.Enemies.Where(c => c.IsAlive).ToArray();
        int damage = attacker.MaxHp;
        await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), targets, damage,
            ValueProp.Unblockable | ValueProp.Unpowered, attacker);
        Trace($"Smoggy before-attack summon={attacker.Monster?.Id.Entry} damage={damage} enemies={targets.Length} attacker-alive={attacker.IsAlive}");
        // AttackCommand checks death after BeforeAttack. Within a monster move
        // also unwind the remaining move so no later attacks/buffs/summons run.
        if (!attacker.IsAlive && ActingMonster.Value?.Creature == attacker)
            throw new SmoggyAttackerDiedException();
    }

    public static void RegisterMove(MonsterModel monster, MoveState move)
    {
        // Canonical/library monsters have no Creature yet; never invoke their
        // throwing Creature getter merely to inspect a move or preload art.
        if (move != null && MonsterCreatureField.GetValue(monster) is Creature owner)
            MoveOwners.AddOrUpdate(move, owner);
    }

    public static IReadOnlyList<AbstractIntent> DisplayIntents(MoveState move, IReadOnlyList<AbstractIntent> original)
    {
        if (!MoveOwners.TryGetValue(move, out var owner) ||
            owner.GetPower<ReflectedTangledPower>() is not { } tangled ||
            (!tangled.IsActive && owner.CombatState.CurrentSide == owner.Side)) return original;
        var result = new List<AbstractIntent>();
        bool added = false;
        foreach (var intent in original)
        {
            if (intent is not AttackIntent) result.Add(intent);
            else if (!added) { result.Add(new ReflectedDefendIntent()); added = true; }
        }
        return result;
    }
}

public sealed class ReflectedDefendIntent : DefendIntent
{
    public decimal Block(Creature owner) => ReflectedEnemyMechanics.TangledBlock(owner);
    public override MegaCrit.Sts2.Core.Localization.LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
    {
        var text = new MegaCrit.Sts2.Core.Localization.LocString("powers", "SNOWFALLENMOD-REFLECTED_TANGLED_POWER.intentLabel");
        text.Add("Block", Block(owner)); return text;
    }
    protected override MegaCrit.Sts2.Core.Localization.LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
    {
        var text = new MegaCrit.Sts2.Core.Localization.LocString("powers", "SNOWFALLENMOD-REFLECTED_TANGLED_POWER.intentDescription");
        text.Add("Block", Block(owner)); return text;
    }
}
