using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using SnowfallenMod.DongmingCode.Cards;
using SnowfallenMod.DongmingCode.Character;

namespace SnowfallenMod.SnowfallenCode.Patches;

[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
public static class DongmingCardMoonlightVisualPatch
{
    private const string VanillaStarIcon = "res://images/packed/sprite_fonts/star_icon.png";
    private const string MoonIcon = "res://Snowfallen/Images/Charui/Moonlight/moon_text_icon.png";
    private static readonly FieldInfo StarIconField = AccessTools.Field(typeof(NCard), "_starIcon");
    private static readonly FieldInfo UnplayableStarIconField = AccessTools.Field(typeof(NCard), "_unplayableStarIcon");
    private static readonly FieldInfo DescriptionLabelField = AccessTools.Field(typeof(NCard), "_descriptionLabel");

    [HarmonyPostfix]
    public static void Postfix(NCard __instance)
    {
        CardModel? model = __instance.Model;

        // Card-library entries are canonical card prototypes and do not have a
        // mutable Owner.  Reading CardModel.Owner here throws
        // CanonicalModelException while the encyclopedia grid is being built.
        // Identify Dongming cards by their model type instead; this works for
        // both normal in-run cards and canonical library cards.
        if (model is not DongmingCard)
            return;

        Texture2D? icon = ResourceLoader.Load<Texture2D>(MoonIcon);
        if (icon is not null)
        {
            (StarIconField?.GetValue(__instance) as TextureRect)?.Set("texture", icon);
            (UnplayableStarIconField?.GetValue(__instance) as TextureRect)?.Set("texture", icon);
        }

        if (DescriptionLabelField?.GetValue(__instance) is MegaCrit.Sts2.addons.mega_text.MegaRichTextLabel label)
        {
            label.Text = label.Text.Replace(VanillaStarIcon, MoonIcon);
        }
    }
}
