using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Powers;

public abstract class JudgingVillainyPowerBase :
    SnowfallenPower,
    IWaterBreathConsumptionListener
{
    /// <summary>每累计消耗这么多次水息触发一次。</summary>
    private const int ConsumptionEventsPerTrigger = 3;

    [SavedProperty]
    public int ConsumedTimesSinceTrigger { get; set; }

    /// <summary>每层判邪佞的力量削减（临时力量，回合结束恢复）。</summary>
    protected abstract decimal StrengthLossPerStack { get; }

    /// <summary>每层判邪佞造成的固定伤害（不吃力量/易伤等加成）。</summary>
    protected abstract decimal DamagePerStack { get; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task AfterWaterBreathConsumed(
        PlayerChoiceContext choiceContext,
        Creature consumer,
        decimal amount,
        CardModel source)
    {
        if (consumer != Owner || amount <= 0m || Owner.CombatState is null)
        {
            return;
        }

        // One successful Consume call counts once, regardless of layers spent.
        ConsumedTimesSinceTrigger++;
        if (ConsumedTimesSinceTrigger >= ConsumptionEventsPerTrigger)
        {
            ConsumedTimesSinceTrigger -= ConsumptionEventsPerTrigger;
            await Trigger(choiceContext, source);
        }
    }

    private async Task Trigger(PlayerChoiceContext choiceContext, CardModel source)
    {
        if (Amount <= 0m || Owner.CombatState is null)
        {
            return;
        }

        List<Creature> livingEnemies = Owner.CombatState.Enemies
            .Where(enemy => enemy.IsAlive)
            .ToList();
        if (livingEnemies.Count == 0)
        {
            return;
        }

        Flash();
        foreach (Creature enemy in livingEnemies)
        {
            await MegaCrit.Sts2.Core.Commands.PowerCmd
                .Apply<JudgingVillainyStrengthDownPower>(
                choiceContext,
                enemy,
                StrengthLossPerStack * Amount,
                Owner,
                source);
        }

        // 固定伤害：Unpowered 表示不吃力量/易伤等能力加成，但仍可被格挡。
        await MegaCrit.Sts2.Core.Commands.CreatureCmd.Damage(
            choiceContext,
            livingEnemies,
            DamagePerStack * Amount,
            DamageProps.nonCardUnpowered,
            Owner,
            source);
    }
}

public class JudgingVillainyPower : JudgingVillainyPowerBase
{
    protected override decimal StrengthLossPerStack => 3m;
    protected override decimal DamagePerStack => 15m;
}

public class UpgradedJudgingVillainyPower : JudgingVillainyPowerBase
{
    protected override decimal StrengthLossPerStack => 4m;
    protected override decimal DamagePerStack => 20m;
}
