using BaseLib.Abstracts;
using Godot;
using SnowfallenCharacter = SnowfallenMod.SnowfallenCode.Character.Snowfallen;

namespace SnowfallenMod.SnowfallenCode.PotionPools;

public partial class SnowfallenPotionPool : CustomPotionPoolModel
{
    public override string EnergyColorName => SnowfallenCharacter.CharacterId;
    public override Color LabOutlineColor => SnowfallenCharacter.Color;
}
