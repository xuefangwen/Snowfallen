using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using SnowfallenMod.DongmingCode.Character;
using SnowfallenMod.DongmingCode.Relics;

namespace SnowfallenMod.DongmingCode.RelicPools;

public partial class DongmingRelicPool : CustomRelicPoolModel
{
    public override string EnergyColorName => Dongming.CharacterId;
    public override Color LabOutlineColor => new("FF78A0");

    public override IEnumerable<RelicModel> GetUnlockedRelics(UnlockState unlockState) =>
        base.GetUnlockedRelics(unlockState)
            .Where(relic => relic is not MingShuang);
}

