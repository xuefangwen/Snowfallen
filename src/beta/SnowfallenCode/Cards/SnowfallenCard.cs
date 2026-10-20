using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Character;
using SnowfallenMod.SnowfallenCode.Extensions;
using SnowfallenMod.SnowfallenCode.Powers;
using SnowfallenCharacter = SnowfallenMod.SnowfallenCode.Character.Snowfallen;

namespace SnowfallenMod.SnowfallenCode.Cards;

[Pool(typeof(SnowfallenCardPool))]
public abstract class SnowfallenCard(int cost, CardType type, CardRarity rarity, TargetType target, bool showInCardLibrary = true) :
    CustomCardModel(cost, type, rarity, target, showInCardLibrary)
{
    public override Task BeforeCombatStart() =>
        SnowfallenMod.SnowfallenCode.Mechanics.CombatHpLossMechanics.Ensure(Owner);

    protected IHoverTip BloodThresholdHoverTip()
    {
        // 图鉴/详情里的卡是 canonical（不可变）模型，CardModel.Owner 的 getter 会 AssertMutable 抛异常，
        // 导致整份 HoverTips 构建失败（悬停时看不到任何索引）。原版 Fasten.cs 同样用 IsMutable 守卫。
        var ownerCreature = IsMutable ? Owner?.Creature : null;
        OmenPower omen = ownerCreature?.Powers
            .OfType<OmenPower>()
            .FirstOrDefault() ?? ModelDb.Power<OmenPower>();
        // 战斗内取玩家身上明兆的实时数值来源（当前最大生命）；
        // 图鉴/详情没有 Owner，退回角色基础生命，保证与 70%/40% 口径一致。
        decimal maxHp = ownerCreature?.MaxHp
            ?? ModelDb.Character<SnowfallenCharacter>().StartingHp;
        int highHp = (int)Math.Ceiling(maxHp * 0.7m);
        int lowHp = (int)Math.Floor(maxHp * 0.4m);
        LocString description = omen.SmartDescription;
        description.Add("HighHp", highHp);
        description.Add("LowHp", lowHp);
        return new HoverTip(
            omen,
            description.GetFormattedText(),
            true);
    }

    //Image size:
    //Normal art: 1000x760 (Using 500x380 should also work, it will simply be scaled.)
    //Full art: 606x852
    public override string CustomPortraitPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath(fallback: false);
            Log.Info(">>>[SnowfallenMod]CardPath=" + path, 2);
            return ResourceLoader.Exists(path) ? path : PortraitPath;
        }
    }

    //Smaller variants of card images for efficiency:
    //Smaller variant of fullart: 250x350
    //Smaller variant of normalart: 250x190

    //Uses card_portraits/card_name.png as image path. These should be smaller images.
    public override string PortraitPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath(fallback: false);
            Log.Info(">>>[SelphinaMod]CardPath=" + path, 2);
            return ResourceLoader.Exists(path) ? path : "card.png".CardImagePath();
        }
    }

    //Optional and I'm not sure it's functional yet.
    public override string BetaPortraitPath
    {
        get
        {
            var path = $"Beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath(fallback: false);
            Log.Info(">>>[SelphinaMod]CardPath=" + path, 2);
            return ResourceLoader.Exists(path) ? path : PortraitPath;
        }
    }
}

