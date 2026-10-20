using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.SnowfallenCode.Enchantments;

/// <summary>
/// 附魔「批注」：带有本附魔的牌进入弃牌堆后，会在拥有者的下一个回合开始时回到手牌
/// （若期间已被抽走或移出弃牌堆，则本次返回自动取消）。
///
/// 待返回标记以可序列化纯值保存，读档后仍在弃牌堆才会返回。
/// </summary>
public class AnnotationMark : CustomEnchantmentModel
{

    private bool _pendingReturn;
    [MegaCrit.Sts2.Core.Saves.Runs.SavedProperty]
    public bool PendingReturn { get => _pendingReturn; set => _pendingReturn = value; }

    protected override string CustomIconPath =>
        Path.Join("Enchantments", "snowfallenmod-annotation.png").ImagePath();

    public override bool ShouldGlowGold => true;

    // 附魔本体也需要至少一条动态变量：原版 EnchantmentModel.DynamicDescription 会
    // DynamicVars.Clone(this) 并在其上做格式化，动态变量集为 null 时会抛 NullReference。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Amount", 1m)];

    /// <summary>
    /// 附魔默认的悬停提示是没有挂模型的普通 HoverTip（只能看、不能点开）。
    /// 这里给出挂了 canonical 模型的版本，悬停时可点击查看该附魔的索引/详情。
    /// </summary>
    public static IHoverTip InspectionTip
    {
        get
        {
            try
            {
                return BuildInspectionTip();
            }
            catch (Exception exception)
            {
                // 最后一道保险：getter 里绝不能抛异常。
                MegaCrit.Sts2.Core.Logging.Log.Warn(
                    $"[Snowfallen.AnnotationMark] inspection tip failed: {exception}");
                return BuildPlainTip();
            }

        }
    }

    private static IHoverTip BuildInspectionTip()
    {
        EnchantmentModel canonical = ModelDb.Enchantment<AnnotationMark>();
        HoverTip tip = new(
            new LocString("enchantments", "SNOWFALLENMOD-ANNOTATION_MARK.title"),
            new LocString("enchantments", "SNOWFALLENMOD-ANNOTATION_MARK.description"),
            LoadIconSafely());
        tip.SetCanonicalModel(canonical);
        tip.Id = canonical.Id.ToString();
        return tip;
    }

    private static IHoverTip BuildPlainTip()
    {
        return new HoverTip(
            new LocString("enchantments", "SNOWFALLENMOD-ANNOTATION_MARK.title"),
            new LocString("enchantments", "SNOWFALLENMOD-ANNOTATION_MARK.description"),
            LoadIconSafely());
    }

    private static Texture2D LoadIconSafely()
    {
        try
        {
            // Never keep a native texture in a static tooltip across asset unloads.
            var icon = ResourceLoader.Load<Texture2D>(
                Path.Join("Enchantments", "snowfallenmod-annotation.png").ImagePath(),
                cacheMode: ResourceLoader.CacheMode.Ignore);
            return GodotObject.IsInstanceValid(icon) ? icon : null;
        }
        catch
        {
            return null;
        }
    }

    public override Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel clonedBy)
    {
        // 只认自己身上这张牌；从其它牌堆进入弃牌堆时挂上待返回标记。
        if (ReferenceEquals(card, Card) &&
            card.Pile?.Type == PileType.Discard &&
            oldPileType != PileType.Discard)
        {
            _pendingReturn = true;
        }

        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!_pendingReturn ||
            Card?.Owner?.Creature is not { } ownerCreature ||
            side != ownerCreature.Side || !participants.Contains(ownerCreature))
        {
            return;
        }

        if (Card.Pile?.Type != PileType.Discard)
        {
            _pendingReturn = false;
            return;
        }

        _pendingReturn = false;
        await CardPileCmd.Add(
            Card,
            PileType.Hand,
            CardPilePosition.Bottom);
    }
}
