using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using SnowfallenMod.DongmingCode.Cards;

namespace SnowfallenMod.DongmingCode.Patches;

[HarmonyPatch(typeof(NHoverTipSet), nameof(NHoverTipSet.SetAlignment))]
public static class DongmingAncientCardPreviewLayoutPatch
{
    private static readonly FieldInfo TextContainer = AccessTools.Field(typeof(NHoverTipSet), "_textHoverTipContainer");
    private static readonly FieldInfo CardContainer = AccessTools.Field(typeof(NHoverTipSet), "_cardHoverTipContainer");

    [HarmonyPostfix]
    public static void Postfix(NHoverTipSet __instance, Control node)
    {
        // Only the event option which previews Dongming's ancient card.
        // Card-library, hand, relic-bar and other characters' tips are untouched.
        if (node is not NEventOptionButton option ||
            option.Option?.HoverTips?.OfType<CardHoverTip>().Any(t => t.Card is EZhou) != true) return;
        Fit(__instance);
        // FlowContainer may finish wrapping its text columns after SetAlignment.
        Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(__instance) && __instance.IsInsideTree()) Fit(__instance);
        }).CallDeferred();
    }

    public static void Fit(NHoverTipSet set)
    {
        if (TextContainer.GetValue(set) is not Control text ||
            CardContainer.GetValue(set) is not Control cards || cards.GetChildCount() == 0) return;
        Vector2 viewport = set.GetViewportRect().Size;
        const float margin = 8f;
        if (viewport.X <= 2 * margin || viewport.Y <= 2 * margin) return;
        // Preserve the original card/text ordering and spacing. Vanilla's
        // overflow correction can move cards to negative X without rechecking.
        foreach (Control container in new[] { text, cards })
        {
            Rect2 rect = container.GetGlobalRect();
            if (rect.Size.Y > viewport.Y - 2 * margin)
                container.Scale *= (viewport.Y - 2 * margin) / rect.Size.Y;
        }
        Rect2 bounds = cards.GetGlobalRect().Merge(text.GetGlobalRect());
        if (bounds.Size.X > viewport.X - 2 * margin)
        {
            float scale = (viewport.X - 2 * margin) / bounds.Size.X;
            foreach (Control container in new[] { text, cards })
            {
                container.GlobalPosition = bounds.Position + (container.GlobalPosition - bounds.Position) * scale;
                container.Scale *= scale;
            }
            bounds = cards.GetGlobalRect().Merge(text.GetGlobalRect());
        }
        float dx = Mathf.Clamp(0, margin - bounds.Position.X, viewport.X - margin - bounds.End.X);
        foreach (Control container in new[] { text, cards })
        {
            Rect2 rect = container.GetGlobalRect();
            float y = Mathf.Clamp(rect.Position.Y, margin, viewport.Y - margin - rect.Size.Y);
            container.GlobalPosition += new Vector2(dx, y - rect.Position.Y);
        }
    }
}
