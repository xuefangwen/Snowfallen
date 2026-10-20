using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
namespace SnowfallenMod.SnowfallenCode.Powers;

public sealed class CombatHpLossPower : SnowfallenPower
{
    [SavedProperty] public int LostHp { get; set; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;
    public override bool ShouldPlayVfx => false;
    internal void Record(int previousHp, int currentHp)
    {
        LostHp += Math.Max(0, previousHp - currentHp);
    }
}
