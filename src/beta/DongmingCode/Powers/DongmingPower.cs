using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.DongmingCode.Powers;

public abstract class DongmingPower : CustomPowerModel
{
    public override string CustomPackedIconPath
    {
        get
        {
            string path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : "power.png".PowerImagePath();
        }
    }

    public override string CustomBigIconPath
    {
        get
        {
            string path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : CustomPackedIconPath;
        }
    }
}

