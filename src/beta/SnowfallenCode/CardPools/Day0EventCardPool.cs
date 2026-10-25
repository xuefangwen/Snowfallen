using BaseLib.Abstracts;
using Godot;

namespace SnowfallenMod.SnowfallenCode.CardPools;

/// <summary>
/// 仅用于注册 DAY0 事件专属卡牌的共享卡池。
/// 不被任何奖励或角色卡池抽取；DAY0 诅咒只由 DAY0 事件直接生成。
/// </summary>
public sealed class Day0EventCardPool : CustomCardPoolModel
{
    public override string Title => "DAY0_EVENT_ONLY";
    public override bool IsShared => true;
    public override bool IsColorless => true;
    public override Color DeckEntryCardColor => new("808080");
    public override Color EnergyOutlineColor => new("404040");
}

