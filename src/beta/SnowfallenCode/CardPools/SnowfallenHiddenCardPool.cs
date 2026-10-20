using SnowfallenMod.SnowfallenCode.Character;

namespace SnowfallenMod.SnowfallenCode.CardPools;

/// <summary>
/// Keeps intentionally removed Snowfallen cards registered without adding
/// them to a playable character's pool or the shared reward pools.
/// </summary>
public sealed class SnowfallenHiddenCardPool : SnowfallenCardPool
{
    public override string Title => "SNOWFALLEN_HIDDEN";
    public override bool IsShared => false;
}
