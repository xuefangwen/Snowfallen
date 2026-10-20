using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace SnowfallenMod.SnowfallenCode.Powers;

public abstract class RidingTheWavesPowerBase : SnowfallenPower
{
    private int _healingTriggersThisTurn;
    private int _lastKnownHp;

    protected abstract decimal FirstDamage { get; }
    protected abstract decimal SecondDamage { get; }
    protected abstract decimal ThirdDamage { get; }

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
            _healingTriggersThisTurn = 0;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner)
        {
            return;
        }

        int currentHp = creature.CurrentHp;
        decimal actualHealing = Math.Max(0, currentHp - _lastKnownHp);
        _lastKnownHp = currentHp;

        if (actualHealing <= 0m || _healingTriggersThisTurn >= 3)
        {
            return;
        }

        _healingTriggersThisTurn++;
        decimal damage = _healingTriggersThisTurn switch
        {
            1 => FirstDamage,
            2 => SecondDamage,
            _ => ThirdDamage
        };

        Flash();
        await CreatureCmd.Damage(
            new BlockingPlayerChoiceContext(),
            CombatState.HittableEnemies,
            damage * Amount,
            DamageProps.nonCardUnpowered,
            Owner);
    }
}

public class RidingTheWavesPower : RidingTheWavesPowerBase
{
    protected override decimal FirstDamage => 2m;
    protected override decimal SecondDamage => 3m;
    protected override decimal ThirdDamage => 5m;
}

public class UpgradedRidingTheWavesPower : RidingTheWavesPowerBase
{
    protected override decimal FirstDamage => 3m;
    protected override decimal SecondDamage => 5m;
    protected override decimal ThirdDamage => 7m;
}

