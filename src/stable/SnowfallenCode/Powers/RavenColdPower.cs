using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Powers;

public class RavenColdPower : SnowfallenPower
{
    private int _cardsPlayedThisTurn;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player)
        {
            return;
        }

        _cardsPlayedThisTurn++;
        decimal healing = _cardsPlayedThisTurn switch
        {
            <= 3 => 2m,
            <= 5 => 1m,
            _ => 0m
        };
        if (healing > 0m)
        {
            decimal effectAmount = healing * Amount;
            Flash();
            await CreatureCmd.Heal(Owner, effectAmount);
            await WaterBreathMechanics.Gain(
                context,
                Owner,
                effectAmount,
                Owner,
                cardPlay.Card);
        }
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (creatures.Contains(Owner))
        {
            _cardsPlayedThisTurn = 0;
        }
        return Task.CompletedTask;
    }
}
