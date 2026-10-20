using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace SnowfallenMod.SnowfallenCode.Powers;

public class OmenPower : SnowfallenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => true;
    public override bool ShouldPlayVfx => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HighHp", 28m),
        new DynamicVar("LowHp", 16m)
    ];

    public override Task BeforeApplied(
        Creature target,
        decimal amount,
        Creature applier,
        CardModel cardSource)
    {
        RefreshThresholds(target);
        return Task.CompletedTask;
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner)
        {
            RefreshThresholds(creature);
        }
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (creatures.Contains(Owner))
        {
            RefreshThresholds(Owner);
        }
        return Task.CompletedTask;
    }

    public void RefreshThresholds()
    {
        RefreshThresholds(Owner);
    }

    private void RefreshThresholds(Creature creature)
    {
        DynamicVars["HighHp"].BaseValue = Math.Ceiling(creature.MaxHp * 0.7m);
        DynamicVars["LowHp"].BaseValue = Math.Floor(creature.MaxHp * 0.4m);
        InvokeDisplayAmountChanged();
    }
}
