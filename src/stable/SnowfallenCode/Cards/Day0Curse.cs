using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.CardPools;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.SnowfallenCode.Cards;

/// <summary>
/// 事件奖励诅咒【DAY0的诅咒】（无费用 · 永恒 · 固有 · 保留 · 不可打出）。
/// 每回合开始时若在手牌中：失去 2 点生命，获得 2 层虚弱和 2 层易伤，失去 1 点能量。
/// 注意触发点是 BeforeHandDraw（回合开始、抽牌之前），所以“在手牌中”指的是
/// 上回合留下来/固有起手的那张，而不是本回合刚抽到的。
/// </summary>
[Pool(typeof(Day0EventCardPool))]
public sealed class Day0Curse() : CustomCardModel(
    // -1 = 不可打出牌的无费用表示，UI 不会画费用球。
    -1,
    CardType.Curse,
    CardRarity.Curse,
    TargetType.None)
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedByModifiers => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Eternal,
        CardKeyword.Innate,
        CardKeyword.Retain,
        CardKeyword.Unplayable
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(2m),
        new PowerVar<WeakPower>(2m),
        new PowerVar<VulnerablePower>(2m),
        new EnergyVar("EnergyLoss", 1)
    ];

    public override string CustomPortraitPath
    {
        get
        {
            string path =
                $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath(fallback: false);
            return ResourceLoader.Exists(path)
                ? path
                : PortraitPath;
        }
    }

    public override string PortraitPath
    {
        get
        {
            string path =
                $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath(fallback: false);
            return ResourceLoader.Exists(path)
                ? path
                : "card.png".CardImagePath();
        }
    }

    public override string BetaPortraitPath => PortraitPath;

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (Owner is null || player != Owner || Pile?.Type != PileType.Hand)
        {
            return;
        }

        await Cmd.Wait(0.25f);
        await SnowfallenMod.SnowfallenCode.Mechanics.SnowfallenHpLoss.Apply(
            choiceContext,
            Owner.Creature,
            DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            this);
        await PowerCmd.Apply<WeakPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["WeakPower"].BaseValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["VulnerablePower"].BaseValue,
            Owner.Creature,
            this);
        await PlayerCmd.LoseEnergy(
            DynamicVars["EnergyLoss"].BaseValue,
            Owner);
    }
}
