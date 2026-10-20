using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class ByTheMilkyWay() : SnowfallenCard(
    1,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    private bool _healedThisTurn;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2)
    ];

    protected override bool IsPlayable => base.IsPlayable && _healedThisTurn;

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner.Creature && delta > 0m)
        {
            _healedThisTurn = true;
        }
        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (creatures.Contains(Owner.Creature))
        {
            _healedThisTurn = false;
        }
        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Dazed dazed = ((CombatState)CombatState).CreateCard<Dazed>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(
            dazed,
            PileType.Hand,
            Owner,
            CardPilePosition.Bottom);

        await PowerCmd.Apply<ByTheMilkyWayPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars.Cards.BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
