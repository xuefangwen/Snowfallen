using MegaCrit.Sts2.Core.Entities.Powers;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.SnowfallenCode.Powers;

public class LakeLightMaxHpPower : SnowfallenPower
{
    public bool IsUsageTracker { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;
}

public class LakeLightEmberDamagePower : SnowfallenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath =>
        "lake_light_max_hp_power.png".PowerImagePath();
    public override string CustomBigIconPath =>
        "lake_light_max_hp_power.png".BigPowerImagePath();
}

