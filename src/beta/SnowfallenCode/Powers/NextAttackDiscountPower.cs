using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace SnowfallenMod.SnowfallenCode.Powers;

public class NextAttackDiscountPower : SnowfallenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool TryModifyEnergyCostInCombat(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner.Creature != Owner ||
            card.Type != CardType.Attack ||
            card.Pile?.Type is not (PileType.Hand or PileType.Play))
        {
            return false;
        }

        modifiedCost = Math.Max(0m, originalCost - Amount);
        return modifiedCost != originalCost;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == Owner &&
            cardPlay.Card.Type == CardType.Attack &&
            cardPlay.Card.Pile?.Type is PileType.Hand or PileType.Play)
        {
            await PowerCmd.Remove(this);
        }
    }
}

