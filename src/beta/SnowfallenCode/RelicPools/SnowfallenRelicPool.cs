using BaseLib.Abstracts;
using Godot;
using SnowfallenCharacter = SnowfallenMod.SnowfallenCode.Character.Snowfallen;

namespace SnowfallenMod.SnowfallenCode.RelicPools;

public partial class SnowfallenRelicPool : CustomRelicPoolModel
{
    public override string EnergyColorName => SnowfallenCharacter.CharacterId;
    public override Color LabOutlineColor => SnowfallenCharacter.Color;
}

