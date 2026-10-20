using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models.PotionPools;
using SnowfallenMod.SnowfallenCode.Extensions;
using SnowfallenMod.SnowfallenCode.PotionPools;

namespace SnowfallenMod.SnowfallenCode.Potions;

[Pool(typeof(SnowfallenPotionPool))]
public abstract class SnowfallenPotion : CustomPotionModel
{
    public override string CustomPackedImagePath
    {
        get
        {
            string path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : "potion.png".PotionImagePath();
        }
    }

    public override string CustomPackedOutlinePath
    {
        get
        {
            string path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".PotionImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : CustomPackedImagePath;
        }
    }
}

[Pool(typeof(SharedPotionPool))]
public abstract class SnowfallenSharedPotion : CustomPotionModel
{
    public override string CustomPackedImagePath
    {
        get
        {
            string path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : "potion.png".PotionImagePath();
        }
    }

    public override string CustomPackedOutlinePath
    {
        get
        {
            string path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".PotionImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : CustomPackedImagePath;
        }
    }
}
