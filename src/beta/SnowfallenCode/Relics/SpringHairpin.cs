using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Saves.Runs;
using SnowfallenMod.SnowfallenCode.Patches;

namespace SnowfallenMod.SnowfallenCode.Relics;

public class SpringHairpin : SnowfallenSharedRelic
{
    [SavedProperty]
    public bool Available { get; set; } = true;

    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override bool IsUsedUp => !Available;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<Sown>(1);

    public override bool TryModifyCardBeingAddedToDeck(
        CardModel card,
        out CardModel newCard)
    {
        newCard = null;
        if (!Available || card is null || !ReferenceEquals(card.Owner, Owner) ||
            !SpringHairpinRewardScope.Contains(card) || card.Enchantment is not null ||
            !ModelDb.Enchantment<Sown>().CanEnchant(card))
        {
            return false;
        }

        // Keep the selected reward card's identity instead of substituting a clone.
        CardCmd.Enchant<Sown>(card, 1m);
        if (card.Enchantment is not Sown) return false;
        newCard = card;
        Available = false;
        SpringHairpinSynchronization.Broadcast(this);
        Flash();
        return true;
    }
}

