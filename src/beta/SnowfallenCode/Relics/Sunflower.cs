using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Relics;

public class Sunflower : SnowfallenSharedRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MingHuangPower>()
    ];

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature dealer,
        CardModel cardSource)
    {
        if (target != Owner.Creature ||
            dealer is null ||
            dealer.Side != CombatSide.Enemy ||
            result.UnblockedDamage <= 0m)
        {
            return;
        }

        decimal currentAmount = dealer.Powers
            .OfType<MingHuangPower>()
            .FirstOrDefault()?.Amount ?? 0m;
        if (currentAmount >= 5m)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<MingHuangPower>(
            choiceContext,
            dealer,
            1m,
            Owner.Creature,
            null);
    }
}

