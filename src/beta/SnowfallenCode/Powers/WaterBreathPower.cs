using MegaCrit.Sts2.Core.Entities.Powers;

namespace SnowfallenMod.SnowfallenCode.Powers;

public class WaterBreathPower : SnowfallenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

