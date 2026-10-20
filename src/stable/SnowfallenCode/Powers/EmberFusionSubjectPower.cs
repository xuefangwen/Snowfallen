using MegaCrit.Sts2.Core.Entities.Powers;

namespace SnowfallenMod.SnowfallenCode.Powers;

/// <summary>
/// Hover-only marker used by an Ember that is waiting to receive a fusion.
/// It is never applied to a creature; keeping the marker on the card instance
/// lets multiple discarded Embers be identified independently.
/// </summary>
public class EmberFusionSubjectPower : SnowfallenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;
}
