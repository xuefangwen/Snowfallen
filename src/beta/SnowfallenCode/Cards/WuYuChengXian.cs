using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.SnowfallenCode.Cards;

/// <summary>
/// 事件奖励牌【我欲成仙】（0 费 · 永恒 · 固有 · 保留 · 无色事件牌池）。
/// 未升级：获得 1 点能量、回复 2 点生命、抽 2 张牌。
/// 升级后：获得 2 点能量、回复 3 点生命、抽 3 张牌。
/// </summary>
[Pool(typeof(EventCardPool))]
public sealed class WuYuChengXian() : CustomCardModel(
    0,
    CardType.Skill,
    // 原版事件池卡牌用的就是这个稀有度；同时让它在图鉴的「其他」分类里可见
    // （图鉴的卡池筛选只认角色池/无色池/远古/稀有度属于 Event/Token/Status/Curse/Quest 的牌）。
    CardRarity.Event,
    TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Eternal, CardKeyword.Innate, CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(1), new HealVar(2m), new CardsVar(2)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [EnergyHoverTip];

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

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        await CardPileCmd.Draw(
            choiceContext,
            DynamicVars.Cards.BaseValue,
            Owner,
            false);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
        DynamicVars.Heal.UpgradeValueBy(1m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

