using BaseLib.Abstracts;
using Godot;
using SnowfallenMod.DongmingCode.Character;

namespace SnowfallenMod.DongmingCode.PotionPools;

public partial class DongmingPotionPool : CustomPotionPoolModel
{
    public override string EnergyColorName => Dongming.CharacterId;
    public override Color LabOutlineColor => new("FF78A0");
}
