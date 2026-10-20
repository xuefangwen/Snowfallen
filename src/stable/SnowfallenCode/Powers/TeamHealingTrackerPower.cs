using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace SnowfallenMod.SnowfallenCode.Powers;

public class TeamHealingTrackerPower : SnowfallenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Healing", 0m),
        new DynamicVar("OwnerHealing", 0m)
    ];

    public decimal TotalHealing => DynamicVars["Healing"].BaseValue;
    public decimal OwnerHealing => DynamicVars["OwnerHealing"].BaseValue;

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (delta <= 0m ||
            creature.Player is null ||
            creature.Side != Owner.Side)
        {
            return Task.CompletedTask;
        }

        DynamicVars["Healing"].BaseValue += delta;
        if (creature == Owner)
        {
            DynamicVars["OwnerHealing"].BaseValue += delta;
        }
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}
