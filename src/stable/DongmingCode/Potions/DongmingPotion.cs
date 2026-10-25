using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using SnowfallenMod.SnowfallenCode.Extensions;
using SnowfallenMod.DongmingCode.PotionPools;

namespace SnowfallenMod.DongmingCode.Potions;

[Pool(typeof(DongmingPotionPool))]
public abstract class DongmingPotion : CustomPotionModel
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
