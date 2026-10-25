using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using SnowfallenMod.DongmingCode.Character;

namespace SnowfallenMod.SnowfallenCode.Patches;

[HarmonyPatch(typeof(NCombatUi), nameof(NCombatUi.Activate))]
public static partial class DongmingMoonlightCounterPatch
{
    private const string BasePath = "res://Snowfallen/Images/Charui/Moonlight/";
    private static readonly FieldInfo StarCounterField =
        AccessTools.Field(typeof(NCombatUi), "_starCounter");
    private static readonly FieldInfo HoverTipField =
        AccessTools.Field(typeof(NStarCounter), "_hoverTip");

    [HarmonyPostfix]
    public static void Postfix(NCombatUi __instance, CombatState state)
    {
        Player player = LocalContext.GetMe(state);
        if (player.Character is not Dongming)
        {
            return;
        }

        NStarCounter? counter = StarCounterField?.GetValue(__instance) as NStarCounter;
        counter ??= __instance.GetNodeOrNull<NStarCounter>("%StarCounter");
        if (counter is null)
        {
            return;
        }

        Texture2D? active = ResourceLoader.Load<Texture2D>(BasePath + "moon_counter_active.png");
        Texture2D? dust = ResourceLoader.Load<Texture2D>(BasePath + "moon_counter_dust.png");
        Texture2D? halo = ResourceLoader.Load<Texture2D>(BasePath + "moon_counter_halo.png");
        if (active is null || dust is null || halo is null)
            return;

        counter.GetNodeOrNull<TextureRect>("Icon")?.Set("texture", active);
        counter.GetNodeOrNull<TextureRect>("Icon/RotationLayers/Layer1")?.Set("texture", dust);
        counter.GetNodeOrNull<TextureRect>("Icon/RotationLayers/Layer2")?.Set("texture", halo);
        if (counter.GetNodeOrNull<TextureRect>("Icon") is TextureRect iconNode)
        {
            iconNode.PivotOffset = iconNode.Size / 2f;
            iconNode.Rotation = -Mathf.DegToRad(30f);
        }
        if (counter.GetNodeOrNull<MoonlightFx>("DongmingMoonlightFx") is null)
            counter.AddChild(new MoonlightFx { Name = "DongmingMoonlightFx" });

        LocString description = new LocString(
            "powers", "SNOWFALLENMOD-MOONLIGHT_COUNTER.description");
        HoverTipField?.SetValue(counter,
            new HoverTip(
                new LocString("powers", "SNOWFALLENMOD-MOONLIGHT_COUNTER.title"),
                description));
    }

    private sealed partial class MoonlightFx : Node
    {
        private double _time;
        public override void _Process(double delta)
        {
            if (GetParent() is not NStarCounter counter) return;
            Node? layers = counter.GetNodeOrNull("Icon/RotationLayers");
            if (layers is not null) ResetRotation(layers);
            TextureRect? icon = counter.GetNodeOrNull<TextureRect>("Icon");
            if (icon is null) return;
            icon.PivotOffset = icon.Size / 2f;
            icon.Rotation = -Mathf.DegToRad(30f);
            _time += delta;
            float glow = 0.94f + 0.06f * (float)(0.5 + 0.5 * System.Math.Sin(_time * 2.4));
            icon.Modulate = new Color(glow, glow, glow, 1f);
            foreach (Node child in icon.GetChildren())
            {
                if (child is CanvasItem layer)
                    layer.Modulate = new Color(1f, 1f, 1f, 0.78f + 0.22f * glow);
            }
        }

        private static void ResetRotation(Node node)
        {
            if (node is Control control)
                control.Rotation = 0f;
            else if (node is Node2D node2D)
                node2D.Rotation = 0f;
            foreach (Node child in node.GetChildren())
                ResetRotation(child);
        }
    }
}

