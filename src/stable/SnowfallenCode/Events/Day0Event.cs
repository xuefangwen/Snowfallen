using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Cards;
using SnowfallenMod.SnowfallenCode.Relics;

namespace SnowfallenMod.SnowfallenCode.Events;

/// <summary>
/// “？”节点事件 DAY0。
/// Acts 留空 = 不绑定章节（全章节共用）；所有角色都可能遇到。
/// 文案用 BaseLib 的内联本地化（EventLoc），不额外新增 json。
/// 选项：
///   1. 坚持就是胜利  → 获得遗物【钢铁之心】
///   2. 放纵今日的狂欢 → 随机获得【我欲成仙】或【DAY0的诅咒】
/// </summary>
// 固定事件 ID（默认会是 SNOWFALLENMOD-DAY0_EVENT）：控制台 /event DAY0 可直接跳转。
[CustomID("DAY0")]
public sealed class Day0Event : CustomEventModel
{
    public override string CustomBackgroundScenePath =>
        "res://Snowfallen/Scenes/Events/background_scenes/day0.tscn";

    // 背景已经铺满整张图，这里给一个透明占位，避免去加载不存在的 <事件ID>.png。
    public override string CustomInitialPortraitPath =>
        "res://Snowfallen/Images/Events/day0_portrait.png";

    public override List<(string, string)> Localization => new EventLoc(
        "DAY0DAY0DAY...",
        new EventPageLoc(
            "INITIAL",
            "琵琶轻放，语声低颤，灭烛来相就。\n" +
            "“禅心已作粘泥絮，不逐春风上下狂”……\n" +
            "“落魄江湖载酒行，楚腰纤细掌中轻”……",
            new EventOptionLoc(
                "PERSIST",
                "坚持就是胜利",
                "获得遗物「钢铁之心」。"),
            new EventOptionLoc(
                "INDULGE",
                "放纵今日的狂欢",
                "随机获得「我欲成仙」或「DAY0的诅咒」。")),
        new EventPageLoc(
            "PERSIST",
            "你把那句话咽了回去，胸口有什么东西冷硬地落了地。\n获得遗物「钢铁之心」。"),
        new EventPageLoc(
            "INDULGE",
            "长夜过去，烛火熄灭时，只留下一件事物。\n随机获得「我欲成仙」或「DAY0的诅咒」。"));

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option(
            Persist,
            "INITIAL",
            HoverTipFactory.FromRelic<IronHeart>().ToArray()),
        Option(
            Indulge,
            "INITIAL",
            [
                .. HoverTipFactory.FromCardWithCardHoverTips<WuYuChengXian>(),
                .. HoverTipFactory.FromCardWithCardHoverTips<Day0Curse>()
            ])
    ];

    private async Task Persist()
    {
        await RelicCmd.Obtain<IronHeart>(Owner);
        SetEventFinished(PageDescription("PERSIST"));
    }

    private async Task Indulge()
    {
        // 用事件自己的确定性 Rng（同种子可复现、多人不同步风险最低），
        // 不用 System.Random。
        CardModel card = Rng.NextBool()
            ? Owner.RunState.CreateCard<WuYuChengXian>(Owner)
            : Owner.RunState.CreateCard<Day0Curse>(Owner);
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.Add(card, PileType.Deck),
            2f);
        SetEventFinished(PageDescription("INDULGE"));
    }
}
