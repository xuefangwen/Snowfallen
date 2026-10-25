using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using SnowfallenMod.DongmingCode.Cards;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.DongmingCode.Powers;

public sealed class YueZhiZuZhouPower : DongmingPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner.Player)
            return;
        await MoonlightMechanics.Gain(player, Amount);
        await PowerCmd.Apply<DoomPower>(choiceContext, Owner, Amount * 2m, Owner, null);
    }
}

public sealed class XuZaiHopePower : DongmingPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner.Player)
            return;
        await MoonlightMechanics.Gain(player, Amount * 4m);
        await PlayerCmd.GainEnergy(Amount * 2m, player);
        await PowerCmd.Remove(this);
    }
}

public sealed class YuLeStrengthDownPower : TemporaryStrengthPower, ICustomPower
{
    public override AbstractModel OriginModel => ModelDb.Card<YuLe>();
    protected override bool IsPositive => false;
    public string CustomPackedIconPath =>
        "yu_le_strength_down_power.png".PowerImagePath();
    public string CustomBigIconPath =>
        "yu_le_strength_down_power.png".BigPowerImagePath();
}

public sealed class MingXinDexterityPower : TemporaryDexterityPower, ICustomPower
{
    public override AbstractModel OriginModel => ModelDb.Card<FuHua>();
    protected override bool IsPositive => true;
    public string CustomPackedIconPath =>
        "ming_xin_dexterity_power.png".PowerImagePath();
    public string CustomBigIconPath =>
        "ming_xin_dexterity_power.png".BigPowerImagePath();
}

public sealed class TongMingStrengthPower : TemporaryStrengthPower, ICustomPower
{
    public override AbstractModel OriginModel => ModelDb.Card<TongMing>();
    protected override bool IsPositive => true;
    public string CustomPackedIconPath =>
        "tong_ming_strength_power.png".PowerImagePath();
    public string CustomBigIconPath =>
        "tong_ming_strength_power.png".BigPowerImagePath();
}
