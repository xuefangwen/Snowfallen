using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Combat;

namespace SnowfallenMod.SnowfallenCode.Powers;

public abstract class HeartShapedSilverPowerBase : SnowfallenPower
{
    private int _triggerCount;

    protected abstract int TriggersPerTurn { get; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task OnEnemyDamageBlockedByFrost(Creature dealer)
    {
        if (dealer is null ||
            dealer.Side != CombatSide.Enemy ||
            _triggerCount >= TriggersPerTurn)
        {
            return;
        }

        _triggerCount++;
        Flash();
            await PowerCmd.Apply<EnergyNextTurnPower>(
            new BlockingPlayerChoiceContext(),
                Owner,
                1m,
                Owner,
                null);
    }

    public override Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (creatures.Contains(Owner))
        {
            _triggerCount = 0;
        }
        return Task.CompletedTask;
    }
}

public class HeartShapedSilverPower : HeartShapedSilverPowerBase
{
    protected override int TriggersPerTurn => 1;
}

public class UpgradedHeartShapedSilverPower : HeartShapedSilverPowerBase
{
    protected override int TriggersPerTurn => 2;
}
