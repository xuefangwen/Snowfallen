using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Powers;

public abstract class WarmReservePowerBase : SnowfallenPower
{
    private int _lastKnownHp;
    private decimal _convertedThisTurn;

    protected abstract decimal FrostCapPerStack { get; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task BeforeApplied(
        Creature target,
        decimal amount,
        Creature applier,
        CardModel cardSource)
    {
        _lastKnownHp = target.CurrentHp;
        return Task.CompletedTask;
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner)
        {
            return;
        }

        int currentHp = creature.CurrentHp;
        decimal actualHealing = Math.Max(0m, currentHp - _lastKnownHp);
        _lastKnownHp = currentHp;

        decimal requestedHealing = Math.Max(0m, delta);
        decimal overflowHealing = Math.Max(0m, requestedHealing - actualHealing);
        decimal remaining = Math.Max(0m, FrostCapPerStack * Amount - _convertedThisTurn);
        decimal converted = Math.Min(overflowHealing, remaining);
        if (converted <= 0m)
        {
            return;
        }

        Flash();
        decimal gained = await FrostMechanics.Gain(
            new BlockingPlayerChoiceContext(),
            Owner,
            converted,
            Owner,
            null);
        _convertedThisTurn += gained;

        if (gained >= 3m)
        {
            await CardPileCmd.Draw(
                new BlockingPlayerChoiceContext(),
                1m,
                Owner.Player);
        }
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Combat.CombatSide side,
        IReadOnlyList<Creature> creatures,
        MegaCrit.Sts2.Core.Combat.ICombatState combatState)
    {
        if (creatures.Contains(Owner))
        {
            _convertedThisTurn = 0m;
            _lastKnownHp = Owner.CurrentHp;
        }
        return Task.CompletedTask;
    }
}

public class WarmReservePower : WarmReservePowerBase
{
    protected override decimal FrostCapPerStack => 8m;
}

public class UpgradedWarmReservePower : WarmReservePowerBase
{
    protected override decimal FrostCapPerStack => 13m;
}

