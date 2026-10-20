using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Cards;
using SnowfallenMod.SnowfallenCode.Extensions;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Enchantments;

public class ExtendingMountain : CustomEnchantmentModel
{
    protected override string CustomIconPath =>
        Path.Join("Enchantments", "snowfallenmod-extending_mountain.png").ImagePath();
    public override bool ShouldGlowGold => true;
    public override bool ShowAmount => true;

    public override bool CanEnchant(CardModel card)
    {
        return card is IEmberCard && base.CanEnchant(card);
    }

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType == CardType.Attack;
    }

    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
    {
        return props.IsPoweredAttack() ? Amount : 0m;
    }
}

