using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Cards;
using SnowfallenMod.SnowfallenCode.Relics;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.SnowfallenCode.Powers;

public class PassingCloudStrengthDownPower : TemporaryStrengthPower, ICustomPower
{
    public override AbstractModel OriginModel => ModelDb.Card<PassingCloud>();
    public string CustomPackedIconPath =>
        "over_cloud_power.png".PowerImagePath();
    public string CustomBigIconPath =>
        "over_cloud_power.png".BigPowerImagePath();
    protected override bool IsPositive => false;
}

public class JudgingVillainyStrengthDownPower :
    TemporaryStrengthPower,
    ICustomPower
{
    public override AbstractModel OriginModel => ModelDb.Card<JudgingVillainy>();
    public string CustomPackedIconPath =>
        "judging_villainy_strength_down_power.png".PowerImagePath();
    public string CustomBigIconPath =>
        "judging_villainy_strength_down_power.png".BigPowerImagePath();
    protected override bool IsPositive => false;
}

public class UnsentSoundStrengthDownPower : TemporaryStrengthPower, ICustomPower
{
    public override AbstractModel OriginModel => ModelDb.Relic<UnsentSound>();
    public string CustomPackedIconPath =>
        "unsent_sound_strength_down_power.png".PowerImagePath();
    public string CustomBigIconPath =>
        "unsent_sound_strength_down_power.png".BigPowerImagePath();
    protected override bool IsPositive => false;
}

public class DeterrentStaffStrengthDownPower : TemporaryStrengthPower, ICustomPower
{
    public override AbstractModel OriginModel => ModelDb.Relic<DeterrentStaff>();
    public string CustomPackedIconPath =>
        "deterrent_staff_strength_down_power.png".PowerImagePath();
    public string CustomBigIconPath =>
        "deterrent_staff_strength_down_power.png".BigPowerImagePath();
    protected override bool IsPositive => false;
}

