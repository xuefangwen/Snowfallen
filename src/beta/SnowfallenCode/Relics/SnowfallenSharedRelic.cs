using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models.RelicPools;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.SnowfallenCode.Relics;

[Pool(typeof(SharedRelicPool))]
public abstract class SnowfallenSharedRelic : CustomRelicModel
{
    public override string PackedIconPath
    {
        get
        {
            string path =
                $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath(fallback: false);
            return ResourceLoader.Exists(path)
                ? path
                : "relic.png".RelicImagePath();
        }
    }

    protected override string PackedIconOutlinePath
    {
        get
        {
            string path =
                $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath(fallback: false);
            return ResourceLoader.Exists(path)
                ? path
                : PackedIconPath;
        }
    }

    protected override string BigIconPath
    {
        get
        {
            string path =
                $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath(fallback: false);
            return ResourceLoader.Exists(path)
                ? path
                : PackedIconPath;
        }
    }
}

