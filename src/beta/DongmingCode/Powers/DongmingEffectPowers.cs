using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.DongmingCode.Mechanics;

namespace SnowfallenMod.DongmingCode.Powers;

public sealed class CursePower : DongmingPower
{
    private const int ExhaustionThreshold = 20;
    private const decimal ModifierPerStack = 0.015m;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageMultiplicative(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature dealer,
        CardModel cardSource,
        CardPlay cardPlay)
    {
        decimal multiplier = 1m;
        decimal stacks = Math.Min(Amount, ExhaustionThreshold - 1);
        if (dealer == Owner && props.HasFlag(ValueProp.Move))
        {
            multiplier *= 1m - stacks * ModifierPerStack;
        }
        if (target == Owner && amount > 0m)
        {
            multiplier *= 1m + stacks * ModifierPerStack;
        }
        return multiplier;
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature applier,
        CardModel cardSource)
    {
        if (!ReferenceEquals(power, this) || Amount < ExhaustionThreshold)
        {
            return;
        }

        Flash();
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<ExhaustionPower>(
            choiceContext,
            Owner,
            3m,
            applier,
            cardSource);
    }
}

public sealed class ExhaustionPower : DongmingPower
{
    private bool _applyingExtraPoisonOrDoom;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageMultiplicative(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature dealer,
        CardModel cardSource,
        CardPlay cardPlay)
    {
        decimal multiplier = 1m;
        if (dealer == Owner && props.HasFlag(ValueProp.Move))
        {
            multiplier *= 0.6m;
        }
        if (target == Owner && amount > 0m)
        {
            multiplier *= 1.6m;
        }
        return multiplier;
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel power,
        Creature target,
        decimal amount,
        Creature applier,
        out decimal modifiedAmount)
    {
        if (target != Owner)
        {
            modifiedAmount = amount;
            return false;
        }

        if (power is CursePower)
        {
            modifiedAmount = 0m;
            return true;
        }

        modifiedAmount = amount;
        return false;
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature applier,
        CardModel cardSource)
    {
        if (_applyingExtraPoisonOrDoom ||
            power.Owner != Owner ||
            amount <= 0m ||
            power is not (PoisonPower or DoomPower))
        {
            return;
        }

        decimal extraAmount = Math.Floor(amount / 2m);
        if (extraAmount <= 0m)
        {
            return;
        }

        _applyingExtraPoisonOrDoom = true;
        try
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                power,
                extraAmount,
                null,
                cardSource);
        }
        finally
        {
            _applyingExtraPoisonOrDoom = false;
        }
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> creatures)
    {
        if (side != Owner.Side || !Owner.IsAlive)
        {
            return;
        }

        // The exhaustion break damage is direct HP loss: it is not an attack,
        // so block, Vulnerable, Vigor and other powered-attack modifiers do
        // not affect it. Resolve it before consuming one duration stack.
        decimal loss = 15m + Math.Floor(Owner.CurrentHp * 0.05m);
        await CreatureCmd.Damage(
            choiceContext,
            Owner,
            loss,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        if (!Owner.IsAlive || !Owner.Powers.Contains(this))
        {
            return;
        }

        await PowerCmd.ModifyAmount(
            choiceContext,
            this,
            -1m,
            Owner,
            null);
    }
}

public sealed class XuShiYinPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStartLate(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        await DongmingSkillMechanics.SelectAndToggle(
            choiceContext,
            player,
            this,
            Amount,
            consumeMark: true);
    }
}

