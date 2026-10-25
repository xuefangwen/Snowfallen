using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using SnowfallenMod.DongmingCode.Powers;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.DongmingCode.Mechanics;

public static class DongmingAttackMechanics
{
    public static bool IsExhausted(Creature target) =>
        target.Powers.OfType<ExhaustionPower>().Any();

    public static decimal DoubleAgainstExhaustion(
        Creature target,
        decimal damage) =>
        IsExhausted(target) ? damage * 2m : damage;

    public static decimal CurseScalingDamage(Creature target, decimal baseDamage)
    {
        bool exhausted = IsExhausted(target);
        decimal stacks = exhausted ? 20m : target.Powers.OfType<CursePower>()
            .Sum(power => (decimal)power.Amount);
        return (baseDamage + stacks) * (exhausted ? 2m : 1m);
    }

    public static int FrostAmount(Player owner) =>
        Math.Max(0, owner.Creature.Powers.OfType<FrostPower>()
            .FirstOrDefault()?.Amount ?? 0);

    public static async Task ConsumeFrost(
        PlayerChoiceContext choiceContext,
        Player owner,
        int amount,
        CardModel source)
    {
        FrostPower frost = owner.Creature.Powers.OfType<FrostPower>()
            .FirstOrDefault();
        if (frost is null || amount <= 0)
        {
            return;
        }

        if (frost.Amount <= amount)
        {
            await PowerCmd.Remove(frost);
        }
        else
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                frost,
                -amount,
                owner.Creature,
                source);
        }
    }

    public static async Task ReduceDoom(
        PlayerChoiceContext choiceContext,
        Player owner,
        decimal amount,
        CardModel source)
    {
        if (amount <= 0m)
        {
            return;
        }

        decimal remaining = amount;
        foreach (PowerModel power in owner.Creature.Powers
                     .Where(power =>
                         power is DoomPower or CalamityPower)
                     .ToList())
        {
            if (remaining <= 0m)
            {
                break;
            }

            decimal removed = Math.Min(remaining, power.Amount);
            await PowerCmd.ModifyAmount(
                choiceContext,
                power,
                -removed,
                owner.Creature,
                source);
            remaining -= removed;
        }
    }

    // 凭心只清除灾厄，不清除已累计的厄劫。
    public static async Task ReduceDoomOnly(
        PlayerChoiceContext choiceContext,
        Player owner,
        decimal amount,
        CardModel source)
    {
        if (amount <= 0m)
        {
            return;
        }

        DoomPower doom = owner.Creature.Powers
            .OfType<DoomPower>()
            .FirstOrDefault();
        if (doom is null)
        {
            return;
        }

        decimal removed = Math.Min(amount, doom.Amount);
        await PowerCmd.ModifyAmount(
            choiceContext,
            doom,
            -removed,
            owner.Creature,
            source);
    }

    public static decimal DoomAmount(Player owner) =>
        owner.Creature.Powers
            .Where(power =>
                power is DoomPower or CalamityPower)
            .Sum(power => (decimal)power.Amount);

    public static Creature RandomEnemy(Player owner) =>
        owner.RunState.Rng.CombatTargets.NextItem(
            owner.Creature.CombatState.HittableEnemies);

    public static async Task ApplyCurse(
        PlayerChoiceContext choiceContext,
        Player owner,
        Creature target,
        decimal amount,
        CardModel source)
    {
        if (amount > 0m && target.IsAlive)
        {
            await PowerCmd.Apply<CursePower>(
                choiceContext,
                target,
                amount,
                owner.Creature,
                source);
        }
    }

    public static async Task MoveSelectedCardsToHand(
        IEnumerable<CardModel> cards)
    {
        foreach (CardModel card in cards)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    public static async Task MoveSelectedCardsToDraw(
        IEnumerable<CardModel> cards)
    {
        foreach (CardModel card in cards)
        {
            await CardPileCmd.Add(card, PileType.Draw);
        }
    }
}

