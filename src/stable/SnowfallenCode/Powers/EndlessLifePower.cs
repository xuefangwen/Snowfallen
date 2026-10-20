using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace SnowfallenMod.SnowfallenCode.Powers;

public abstract class EndlessLifePowerBase : SnowfallenPower
{
    [SavedProperty] public int AccumulatedHpLoss { get; set; }
    [SavedProperty] public int LastKnownHp { get; set; }
    private bool _resolving;
    protected abstract decimal HealingPerStack { get; }
    protected abstract decimal StrengthPerStack { get; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task BeforeApplied(Creature target, decimal amount, Creature applier, CardModel cardSource)
    {
        LastKnownHp = target.CurrentHp;
        return Task.CompletedTask;
    }

    internal void RecordHpChange(int currentHp)
    {
        AccumulatedHpLoss += Math.Max(0, LastKnownHp - currentHp);
        LastKnownHp = currentHp;
    }

    internal void RecordCommittedHpChange(int previousHp, int currentHp)
    {
        LastKnownHp = previousHp;
        RecordHpChange(currentHp);
    }

    // Temporary max-HP expiration clamps HP without damage or an HP-change hook.
    internal void SynchronizeHpWithoutLoss() => LastKnownHp = Owner.CurrentHp;

    internal bool TakeTrigger()
    {
        if (AccumulatedHpLoss < 20) return false;
        AccumulatedHpLoss -= 20;
        return true;
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner) return;
        if (_resolving || creature.IsDead) return;
        _resolving = true;
        try
        {
            var context = new BlockingPlayerChoiceContext();
            while (!Owner.IsDead && TakeTrigger())
            {
                Flash();
                await PowerCmd.Apply<StrengthPower>(context, Owner, StrengthPerStack * Amount, Owner, null);
                await CreatureCmd.Heal(Owner, HealingPerStack * Amount);
            }
        }
        finally { _resolving = false; }
    }
}

public class EndlessLifePower : EndlessLifePowerBase
{
    protected override decimal HealingPerStack => 3m;
    protected override decimal StrengthPerStack => 2m;
}

public class UpgradedEndlessLifePower : EndlessLifePowerBase
{
    protected override decimal HealingPerStack => 5m;
    protected override decimal StrengthPerStack => 2m;
}
