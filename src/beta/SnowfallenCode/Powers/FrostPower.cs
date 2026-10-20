using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace SnowfallenMod.SnowfallenCode.Powers;

public class FrostPower : SnowfallenPower
{
    private decimal _pendingAbsorption;
    private Creature _pendingDealer;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyHpLostBeforeOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature dealer,
        CardModel cardSource)
    {
        if (target != Owner ||
            amount <= 0m ||
            dealer is null ||
            dealer.Side != CombatSide.Enemy ||
            Owner.Powers.OfType<TemperingPower>().Any())
        {
            return amount;
        }

        _pendingAbsorption = Math.Min(amount, Amount);
        _pendingDealer = dealer;
        return amount - _pendingAbsorption;
    }

    public override async Task AfterModifyingHpLostBeforeOsty()
    {
        decimal absorbed = _pendingAbsorption;
        Creature dealer = _pendingDealer;
        _pendingAbsorption = 0m;
        _pendingDealer = null;

        if (absorbed <= 0m)
        {
            return;
        }

        Flash();
        await PowerCmd.ModifyAmount(
            new BlockingPlayerChoiceContext(),
            this,
            -absorbed,
            Owner,
            null);

        foreach (HeartShapedSilverPowerBase power in Owner.Powers
                     .OfType<HeartShapedSilverPowerBase>()
                     .ToList())
        {
            await power.OnEnemyDamageBlockedByFrost(dealer);
        }
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner && Amount > creature.MaxHp)
        {
            await PowerCmd.ModifyAmount(
                new BlockingPlayerChoiceContext(),
                this,
                creature.MaxHp - Amount,
                Owner,
                null);
        }
    }

}

