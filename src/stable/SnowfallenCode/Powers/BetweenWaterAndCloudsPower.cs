using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Powers;

public abstract class BetweenWaterAndCloudsPowerBase : SnowfallenPower
{
    private int _pendingEnemyHits;

    protected abstract decimal DamageMultiplier { get; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageMultiplicative(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature dealer,
        CardModel cardSource)
    {
        if (target != Owner ||
            dealer is null ||
            dealer.Side != CombatSide.Enemy ||
            !props.HasFlag(ValueProp.Move) ||
            props.HasFlag(ValueProp.Unpowered))
        {
            return 1m;
        }

        return DamageMultiplier;
    }

    public override Task BeforeDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature dealer,
        CardModel cardSource)
    {
        if (target == Owner &&
            amount > 0m &&
            dealer is not null &&
            dealer.Side == CombatSide.Enemy &&
            props.HasFlag(ValueProp.Move) &&
            !props.HasFlag(ValueProp.Unpowered))
        {
            _pendingEnemyHits++;
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
        if (target != Owner || _pendingEnemyHits <= 0)
        {
            return;
        }

        _pendingEnemyHits--;
        Flash();
        await EmberMechanics.CreateInHand(
            Owner.Player,
            CombatState,
            1);
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

public class BetweenWaterAndCloudsPower : BetweenWaterAndCloudsPowerBase
{
    protected override decimal DamageMultiplier => 0.7m;
}

public class UpgradedBetweenWaterAndCloudsPower : BetweenWaterAndCloudsPowerBase
{
    protected override decimal DamageMultiplier => 0.5m;
}
