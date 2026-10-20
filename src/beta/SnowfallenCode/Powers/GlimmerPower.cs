using MegaCrit.Sts2.Core.Entities.Powers;

namespace SnowfallenMod.SnowfallenCode.Powers;

public abstract class GlimmerPowerBase : SnowfallenPower
{
    public abstract decimal ExtraBlockPerStack { get; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

public class GlimmerPower : GlimmerPowerBase
{
    public override decimal ExtraBlockPerStack => 2m;
}

public class UpgradedGlimmerPower : GlimmerPowerBase
{
    public override decimal ExtraBlockPerStack => 3m;
}

