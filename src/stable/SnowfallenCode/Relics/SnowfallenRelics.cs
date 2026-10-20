using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using SnowfallenMod.SnowfallenCode.Extensions;
using SnowfallenMod.SnowfallenCode.RelicPools;

namespace SnowfallenMod.SnowfallenCode.Relics;

[Pool(typeof(SnowfallenRelicPool))]
public abstract class SnowfallenRelics : CustomRelicModel
{
    public override string PackedIconPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : "relic.png".RelicImagePath();
        }
    }

    protected override string PackedIconOutlinePath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : PackedIconPath;
        }
    }

    protected override string BigIconPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : PackedIconPath;
        }
    }
}
