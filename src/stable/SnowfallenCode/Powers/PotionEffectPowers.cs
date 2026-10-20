using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Powers;

public class RiverBeaverPower : SnowfallenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        Flash();
        await PlayerCmd.GainEnergy(2m, player);
        await PowerCmd.ModifyAmount(
            new BlockingPlayerChoiceContext(),
            this,
            -1m,
            Owner,
            null);
    }
}

public class InvestorPower : SnowfallenPower
{
    private readonly List<int> _remainingTurns = [];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => _remainingTurns.Count;

    public static async Task RegisterUse(
        PlayerChoiceContext choiceContext,
        Creature owner,
        Creature applier)
    {
        InvestorPower power = owner.Powers.OfType<InvestorPower>().FirstOrDefault();
        if (power is null)
        {
            await PowerCmd.Apply<InvestorPower>(
                choiceContext,
                owner,
                1m,
                applier,
                null);
            power = owner.Powers.OfType<InvestorPower>().FirstOrDefault();
        }
        else
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                power,
                1m,
                applier,
                null);
        }

        power?._remainingTurns.Add(3);
    }

    internal IReadOnlyList<int> GetVictoryGoldRewards()
    {
        return Enumerable.Repeat(50, _remainingTurns.Count).ToArray();
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || _remainingTurns.Count == 0)
        {
            return;
        }

        int expired = 0;
        for (int index = _remainingTurns.Count - 1; index >= 0; index--)
        {
            int remaining = _remainingTurns[index] - 1;
            if (remaining > 0)
            {
                _remainingTurns[index] = remaining;
                continue;
            }

            _remainingTurns.RemoveAt(index);
            expired++;
        }

        if (expired <= 0)
        {
            return;
        }

        Flash();
        for (int index = 0; index < expired; index++)
        {
            await PlayerCmd.LoseGold(20m, Owner.Player);
        }

        await PowerCmd.ModifyAmount(
            choiceContext,
            this,
            -expired,
            Owner,
            null);
    }
}

public class LurkingAftereffectPower : SnowfallenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, 2m, Owner, null);
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, 2m, Owner, null);
        await PowerCmd.Remove(this);
    }
}

public class PotatoProtectionPower : SnowfallenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyDamageMultiplicative(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature dealer,
        CardModel cardSource)
    {
        return target == Owner &&
               dealer is not null &&
               dealer.Side == CombatSide.Enemy &&
               props.HasFlag(ValueProp.Move) &&
               !props.HasFlag(ValueProp.Unpowered)
            ? 0.5m
            : 1m;
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (creatures.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}

public class AncientWinePower : SnowfallenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyDamageMultiplicative(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature dealer,
        CardModel cardSource)
    {
        if (dealer == Owner || target == Owner)
        {
            return 2m;
        }

        return 1m;
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (creatures.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}

public class ThunderBowlPower : SnowfallenPower
{
    private sealed class ThunderRecord
    {
        public bool IsCounting { get; set; } = true;
        public int Kills { get; set; }
    }

    private readonly List<ThunderRecord> _records = [];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => _records.Sum(record => record.Kills);

    public static async Task RegisterUse(
        PlayerChoiceContext choiceContext,
        Creature owner,
        Creature applier)
    {
        ThunderBowlPower power = owner.Powers.OfType<ThunderBowlPower>().FirstOrDefault();
        if (power is null)
        {
            await PowerCmd.Apply<ThunderBowlPower>(
                choiceContext,
                owner,
                1m,
                applier,
                null);
            power = owner.Powers.OfType<ThunderBowlPower>().FirstOrDefault();
        }
        else
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                power,
                1m,
                applier,
                null);
        }

        power?._records.Add(new ThunderRecord());
    }

    internal IReadOnlyList<int> GetVictoryGoldRewards()
    {
        return _records
            .Where(record => record.Kills > 0)
            .Select(record => record.Kills * 15)
            .ToArray();
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (wasRemovalPrevented || creature.Side != CombatSide.Enemy)
        {
            return;
        }

        bool changed = false;
        foreach (ThunderRecord record in _records.Where(record => record.IsCounting))
        {
            record.Kills++;
            changed = true;
        }

        if (changed)
        {
            Flash();
        }
    }

    public override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
        {
            foreach (ThunderRecord record in _records)
            {
                record.IsCounting = false;
            }
        }

        return Task.CompletedTask;
    }
}

public class IronDefenseWatchPower : SnowfallenPower
{
    private int _blockBefore;
    private decimal _frostBefore;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override Task BeforeDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature dealer,
        CardModel cardSource)
    {
        if (target == Owner &&
            dealer is not null &&
            dealer.Side == CombatSide.Enemy &&
            props.HasFlag(ValueProp.Move))
        {
            _blockBefore = target.Block;
            _frostBefore = target.Powers.OfType<FrostPower>().FirstOrDefault()?.Amount ?? 0m;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature dealer,
        CardModel cardSource)
    {
        if (target != Owner ||
            dealer is null ||
            dealer.Side != CombatSide.Enemy ||
            !props.HasFlag(ValueProp.Move))
        {
            return;
        }

        decimal frostAfter = target.Powers.OfType<FrostPower>().FirstOrDefault()?.Amount ?? 0m;
        bool crossedBlock = _blockBefore > 0 && result.UnblockedDamage > 0;
        bool reachedFrost = _frostBefore > frostAfter;
        if (!crossedBlock && !reachedFrost)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<ArtifactPower>(choiceContext, Owner, 1m, Owner, null);
        await PowerCmd.Remove(this);
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (creatures.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
