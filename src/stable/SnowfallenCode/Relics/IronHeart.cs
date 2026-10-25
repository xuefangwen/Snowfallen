using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.SnowfallenCode.Relics;

/// <summary>
/// 事件遗物【钢铁之心】（只能从 DAY0 事件获得，不进普通遗物掉落池）。
/// 获得时：生命上限 +7（GainMaxHp 本身会同步回复等量生命）。
/// 每场战斗开始时：获得 2 点力量。
/// 下一场战斗开始时：额外获得 2 层虚弱（只在获得后的第一场战斗生效）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public sealed class IronHeart : CustomRelicModel
{
    /// <summary>是否还欠着“下一场战斗的虚弱”。存档保存，避免退游戏后失效。</summary>
    [SavedProperty]
    public bool PendingFirstCombatWeakness { get; set; } = true;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new MaxHpVar(7m),
        new PowerVar<WeakPower>(2m),
        new PowerVar<StrengthPower>(2m)
    ];

    public override string PackedIconPath => RelicIcon(".png");

    protected override string PackedIconOutlinePath => RelicIcon("_outline.png");

    protected override string BigIconPath => BigRelicIcon();

    public override async Task AfterObtained()
    {
        Flash();
        await CreatureCmd.GainMaxHp(Owner.Creature, DynamicVars.MaxHp.BaseValue);
    }

    public override async Task BeforeCombatStart()
    {
        Flash();
        PlayerChoiceContext choiceContext = new BlockingPlayerChoiceContext();
        if (PendingFirstCombatWeakness)
        {
            PendingFirstCombatWeakness = false;
            await PowerCmd.Apply<WeakPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["WeakPower"].BaseValue,
                Owner.Creature,
                null);
        }

        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["StrengthPower"].BaseValue,
            Owner.Creature,
            null);
    }

    private string RelicIcon(string suffix)
    {
        string name = Id.Entry.RemovePrefix().ToLowerInvariant();
        string path = $"{name}{suffix}".RelicImagePath(fallback: false);
        return ResourceLoader.Exists(path)
            ? path
            : suffix.StartsWith("_") ? PackedIconPath : "relic.png".RelicImagePath();
    }

    private string BigRelicIcon()
    {
        string name = Id.Entry.RemovePrefix().ToLowerInvariant();
        string path = $"{name}.png".BigRelicImagePath(fallback: false);
        return ResourceLoader.Exists(path)
            ? path
            : PackedIconPath;
    }
}
