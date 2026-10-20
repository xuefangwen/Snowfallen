using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Mechanics;

/// <summary>
/// 水息被消耗后的通知接口。判邪佞（每消耗3次水息触发）靠它统计"任意来源"的消耗，
/// 不必给每个消耗点单独打补丁。
/// </summary>
public interface IWaterBreathConsumptionListener
{
    Task AfterWaterBreathConsumed(
        PlayerChoiceContext choiceContext,
        Creature consumer,
        decimal amount,
        CardModel source);
}

public static class WaterBreathMechanics
{
    public static bool WasGainedThisTurn(Player owner, ICombatState combatState)
    {
        CombatState state = (CombatState)combatState;
        return CombatManager.Instance.History.Entries
            .OfType<PowerReceivedEntry>()
            .Any(entry =>
                entry.HappenedThisTurn(state) &&
                entry.Actor == owner.Creature &&
                entry.Power is WaterBreathPower &&
                entry.Amount > 0m);
    }

    public static int TimesGainedThisTurn(Player owner, ICombatState combatState)
    {
        CombatState state = (CombatState)combatState;
        return CombatManager.Instance.History.Entries
            .OfType<PowerReceivedEntry>()
            .Count(entry =>
                entry.HappenedThisTurn(state) &&
                entry.Actor == owner.Creature &&
                entry.Power is WaterBreathPower &&
                entry.Amount > 0m);
    }

    public static Task Gain(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        Creature applier,
        CardModel source)
    {
        if (amount <= 0m)
        {
            return Task.CompletedTask;
        }

        return PowerCmd.Apply<WaterBreathPower>(
            choiceContext,
            target,
            amount,
            applier,
            source);
    }

    public static async Task<decimal> Consume(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal maximum,
        CardModel source)
    {
        WaterBreathPower waterBreath = target.Powers
            .OfType<WaterBreathPower>()
            .FirstOrDefault();
        decimal consumed = Math.Min(Math.Max(0m, maximum), waterBreath?.Amount ?? 0m);
        if (waterBreath is not null && consumed > 0m)
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                waterBreath,
                -consumed,
                target,
                source);

            // 通知持有者身上的监听者（如判邪佞）。先取快照，避免监听者在回调里
            // 改动能力列表时出现迭代问题。
            List<IWaterBreathConsumptionListener> listeners = target.Powers
                .OfType<IWaterBreathConsumptionListener>()
                .ToList();
            foreach (IWaterBreathConsumptionListener listener in listeners)
            {
                await listener.AfterWaterBreathConsumed(
                    choiceContext,
                    target,
                    consumed,
                    source);
            }
        }

        return consumed;
    }
}
