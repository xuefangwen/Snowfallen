using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace SnowfallenMod.SnowfallenCode.Powers;

public class FinalStrokePower : SnowfallenPower
{
    private int _exhaustedEmberRemainder;
    private int _pendingEnergyNextTurn;
    private CardModel _pendingSource;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public void RecordExhaustedEmber(CardModel source)
    {
        _exhaustedEmberRemainder++;
        if (_exhaustedEmberRemainder >= 2)
        {
            _exhaustedEmberRemainder -= 2;
            _pendingEnergyNextTurn++;
            _pendingSource = source;
            Flash();
        }

        InvokeDisplayAmountChanged();
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        // Exhaust callbacks may run while the engine is still moving the
        // source card between piles. Applying a power from inside that
        // callback can nest another command into the unfinished pile move and
        // leave the play pipeline waiting forever. Flush only after the outer
        // card play has completed.
        await FlushPendingEnergy(choiceContext);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> creatures)
    {
        if (side == Owner.Side)
        {
            // Ethereal exhausts happen during turn cleanup rather than inside
            // a card play, so this is their safe flush point.
            await FlushPendingEnergy(choiceContext);
        }
    }

    private async Task FlushPendingEnergy(PlayerChoiceContext choiceContext)
    {
        if (_pendingEnergyNextTurn <= 0)
        {
            return;
        }

        int amount = _pendingEnergyNextTurn;
        CardModel source = _pendingSource;
        _pendingEnergyNextTurn = 0;
        _pendingSource = null;
        await PowerCmd.Apply<EnergyNextTurnPower>(
            choiceContext,
            Owner,
            amount,
            Owner,
            source);
    }
}

