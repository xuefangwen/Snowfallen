using System.Reflection;
using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Runs;
using SnowfallenMod.SnowfallenCode.Extensions;
using SnowfallenCharacter = SnowfallenMod.SnowfallenCode.Character.Snowfallen;

namespace SnowfallenMod.SnowfallenCode.Patches;

[HarmonyPatch(typeof(NInspectRelicScreen), "UpdateRelicDisplay")]
public static class SnowfallenYummyCookieInspectPatch
{
    private static readonly FieldInfo RelicsField =
        AccessTools.Field(typeof(NInspectRelicScreen), "_relics");

    private static readonly FieldInfo IndexField =
        AccessTools.Field(typeof(NInspectRelicScreen), "_index");

    [HarmonyPostfix]
    public static void Postfix(NInspectRelicScreen __instance)
    {
        Player localPlayer = LocalContext.GetMe(
            RunManager.Instance.DebugOnlyGetState());
        if (localPlayer?.Character is not SnowfallenCharacter)
        {
            return;
        }

        var relics = RelicsField.GetValue(__instance) as
            IReadOnlyList<RelicModel>;
        int index = (int)IndexField.GetValue(__instance);
        if (relics is null ||
            index < 0 ||
            index >= relics.Count ||
            relics[index] is not YummyCookie)
        {
            return;
        }

        TextureRect relicImage =
            __instance.GetNode<TextureRect>("%RelicImage");
        relicImage.Texture = PreloadManager.Cache.GetTexture2D(
            "yummy_cookie_snowfallen.png".BigRelicImagePath());
    }
}
