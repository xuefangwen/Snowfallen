using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.SnowfallenCode.Powers;

public abstract class SnowfallenPower : CustomPowerModel
{
    public override string CustomPackedIconPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : "power.png".PowerImagePath();
        }
    }

    public override string CustomBigIconPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : CustomPackedIconPath;
        }
    }
}

