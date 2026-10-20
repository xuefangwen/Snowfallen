using Godot;
using MainFile = SnowfallenMod.Snowfallen.MainFile;

namespace SnowfallenMod.SnowfallenCode.Extensions;

//Mostly utilities to get asset paths.
public static class StringExtensions
{
    public static string DefaultImagePath => $"{MainFile.ModId}/Images/default_nope.png";

    // Resolve NOPE only for a final path. Candidate probes must pass fallback: false
    // so a missing phase, large portrait or outline does not mask existing art.
    private static string ResolveImagePath(string path, bool fallback) =>
        !fallback || ResourceLoader.Exists(path) ? path : DefaultImagePath;

    public static string ImagePath(this string path, bool fallback = true)
    {
        return ResolveImagePath(Path.Join(MainFile.ModId, "Images", path), fallback);
    }

    public static string CardImagePath(this string path, bool fallback = true)
    {
        return ResolveImagePath(Path.Join(MainFile.ModId, "Images", "Cards", path), fallback);
    }

    public static string BigCardImagePath(this string path, bool fallback = true)
    {
        return ResolveImagePath(Path.Join(MainFile.ModId, "Images", "Cards", "Big", path), fallback);
    }

    public static string PowerImagePath(this string path, bool fallback = true)
    {
        return ResolveImagePath(Path.Join(MainFile.ModId, "Images", "Powers", path), fallback);
    }
    public static string BigPowerImagePath(this string path, bool fallback = true)
    {
        return ResolveImagePath(Path.Join(MainFile.ModId, "Images", "Powers", "Big", path), fallback);
    }

    public static string RelicImagePath(this string path, bool fallback = true)
    {
        return ResolveImagePath(Path.Join(MainFile.ModId, "Images", "Relics", path), fallback);
    }

    public static string BigRelicImagePath(this string path, bool fallback = true)
    {
        return ResolveImagePath(Path.Join(MainFile.ModId, "Images", "Relics", "Big", path), fallback);
    }

    public static string PotionImagePath(this string path, bool fallback = true)
    {
        return ResolveImagePath(Path.Join(MainFile.ModId, "Images", "Potions", path), fallback);
    }

    public static string CharacterUiPath(this string path, bool fallback = true)
    {
        return ResolveImagePath(Path.Join(MainFile.ModId, "Images", "Charui", path), fallback);
    }
}
