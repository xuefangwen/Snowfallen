using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Powers;

public abstract class SoulSeparationArtPowerBase : SnowfallenPower
{
    private int _lastKnownHp;
    private bool _resolving;

    protected abstract decimal HealPerStack { get; }

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

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (creatures.Contains(Owner))
        {
            _lastKnownHp = Owner.CurrentHp;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner)
        {
            return;
        }

        int oldHp = _lastKnownHp;
        int currentHp = creature.CurrentHp;
        _lastKnownHp = currentHp;
        if (_resolving || oldHp == currentHp || Owner.Player is null)
        {
            return;
        }

        decimal oldRatio = (decimal)oldHp / creature.MaxHp;
        decimal newRatio = (decimal)currentHp / creature.MaxHp;
        bool crossedHigh =
            oldRatio < 0.7m && newRatio >= 0.7m ||
            oldRatio >= 0.7m && newRatio < 0.7m;
        bool crossedLow =
            oldRatio <= 0.4m && newRatio > 0.4m ||
            oldRatio > 0.4m && newRatio <= 0.4m;
        if (!crossedHigh && !crossedLow)
        {
            return;
        }

        _resolving = true;
        try
        {
            Flash();
            await EmberMechanics.CreateInHand(
                Owner.Player,
                Owner.CombatState,
                Amount);
            await CreatureCmd.Heal(Owner, HealPerStack * Amount);
        }
        finally
        {
            _lastKnownHp = Owner.CurrentHp;
            _resolving = false;
        }
    }
}

public class SoulSeparationArtPower : SoulSeparationArtPowerBase
{
    protected override decimal HealPerStack => 5m;
}

public class UpgradedSoulSeparationArtPower : SoulSeparationArtPowerBase
{
    protected override decimal HealPerStack => 7m;
}

