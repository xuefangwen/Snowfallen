using Godot;
using HarmonyLib;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using SnowfallenMod.SnowfallenCode.Character;

namespace SnowfallenMod.SnowfallenCode.Patches;

[HarmonyPatch(typeof(NEnergyCounter), "OnEnergyChanged")]
internal static partial class SnowfallenEnergyCounterPatch
{
    private static readonly FieldInfo PlayerField =
        AccessTools.Field(typeof(NEnergyCounter), "_player");

    internal static bool IsSnowfallen(NEnergyCounter counter) =>
        PlayerField?.GetValue(counter) is Player { Character: SnowfallenMod.SnowfallenCode.Character.Snowfallen };

    private static void Postfix(NEnergyCounter __instance)
    {
        if (!IsSnowfallen(__instance)) return;
        if (__instance.GetNodeOrNull<SnowflakeFlicker>("SnowflakeFlicker") is null)
        {
            __instance.AddChild(new SnowflakeFlicker { Name = "SnowflakeFlicker" });
        }
    }

    internal sealed partial class SnowflakeFlicker : Node
    {
        private double _time;
        public override void _Process(double delta)
        {
            if (GetParent() is not NEnergyCounter counter) return;
            Node? layers = counter.GetNodeOrNull("Layers");
            if (layers is not CanvasItem canvas) return;
            _time += delta;
            float glow = 0.94f + 0.06f * (float)(0.5 + 0.5 * System.Math.Sin(_time * 2.4));
            canvas.Modulate = new Color(glow, glow, glow, 1f);
        }
    }
}

[HarmonyPatch(typeof(NEnergyCounter), nameof(NEnergyCounter._Ready))]
internal static class SnowfallenEnergyCounterReadyPatch
{
    private static void Postfix(NEnergyCounter __instance)
    {
        if (!SnowfallenEnergyCounterPatch.IsSnowfallen(__instance)) return;
        if (__instance.GetNodeOrNull<SnowfallenEnergyCounterPatch.SnowflakeFlicker>("SnowflakeFlicker") is null)
            __instance.AddChild(new SnowfallenEnergyCounterPatch.SnowflakeFlicker { Name = "SnowflakeFlicker" });
    }
}

// 雪放能量点：只保留静态图层，取消 layer2 / layer3 的旋转。
// 原版 NEnergyCounter._Process 会按 (i + 1) 的倍率旋转 %RotationLayers 的子节点，
// 而该方法除旋转外不做任何事，所以对雪放直接跳过；其他角色（含东铭）保持不变。
[HarmonyPatch(typeof(NEnergyCounter), "_Process", new[] { typeof(double) })]
internal static class SnowfallenEnergyCounterNoRotationPatch
{
    [HarmonyPrefix]
    private static bool Prefix(NEnergyCounter __instance) =>
        !SnowfallenEnergyCounterPatch.IsSnowfallen(__instance);
}
