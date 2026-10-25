using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.DongmingCode.Powers;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.DongmingCode.Potions;

public sealed class MoonPotion : DongmingPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature target) =>
        MoonlightMechanics.Gain(Owner, 3m);
}

public sealed class WhiteFrostPotion : DongmingPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        await FrostMechanics.Gain(
            choiceContext, Owner.Creature, 15m, Owner.Creature, null);
        await PowerCmd.Apply<NingHuaPower>(
            choiceContext, Owner.Creature, 3m, Owner.Creature, null);
    }
}

public sealed class BloodlineElixir : DongmingPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        await PowerCmd.Apply<XuShiYinPower>(
            choiceContext, Owner.Creature, 5m, Owner.Creature, null);
        XuShiYinPower marks = Owner.Creature.Powers
            .OfType<XuShiYinPower>()
            .FirstOrDefault();
        if (marks is not null)
        {
            // 立即触发的上限取当前总层数（原来是硬编码 5）
            await DongmingSkillMechanics.SelectAndToggleWithCurrentMarks(
                choiceContext, Owner, marks, consumeMark: true);
        }
    }
}

