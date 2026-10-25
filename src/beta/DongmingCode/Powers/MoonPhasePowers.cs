using MegaCrit.Sts2.Core.Entities.Powers;

namespace SnowfallenMod.DongmingCode.Powers;

public sealed class DongmingBrightMoonPhasePower : DongmingPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;
    public override bool ShouldPlayVfx => false;
}

public sealed class DongmingDarkMoonPhasePower : DongmingPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;
    public override bool ShouldPlayVfx => false;
}

