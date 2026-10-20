using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

[BaseLib.Utils.Pool(typeof(SnowfallenMod.SnowfallenCode.Character.RetiredSnowfallenCardPool))]
public class SkipReading() : SnowfallenCard(
    1,
    CardType.Skill,
    CardRarity.Common,
    TargetType.Self, false)
{
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override HashSet<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
        new EnergyVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardSelectorPrefs prefs = new(
            SelectionScreenPrompt,
            0,
            DynamicVars.Cards.IntValue)
        {
            PretendCardsCanBePlayed = true
        };
        List<CardModel> selected = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            prefs,
            card =>
                !ReferenceEquals(card, this) &&
                !card.Keywords.Contains(CardKeyword.Unplayable) &&
                !card.EnergyCost.CostsX &&
                card.EnergyCost.GetWithModifiers(CostModifiers.All) <=
                DynamicVars.Energy.IntValue,
            this)).ToList();

        List<IEmberCard> reservedEmbers = selected.OfType<IEmberCard>().ToList();
        EmberTrackerPower emberTracker = null;
        if (reservedEmbers.Count > 0)
        {
            emberTracker = await EmberMechanics.EnsureTracker(
                choiceContext,
                Owner,
                CombatState,
                this);
            emberTracker.ReserveFromFusion(reservedEmbers);
        }

        try
        {
            foreach (CardModel card in selected)
            {
                Creature target = ResolveDefaultTarget(card);
                if (RequiresTarget(card) && target is null)
                {
                    continue;
                }

                await CardPileCmd.Add(card, PileType.Play);
                if (card is IEmberCard ember)
                {
                    emberTracker?.ReleaseFromFusion(ember);
                }
                await CardCmd.AutoPlay(choiceContext, card, target);
            }
        }
        finally
        {
            emberTracker?.ReleaseFromFusion(reservedEmbers);
        }
    }

    private Creature ResolveDefaultTarget(CardModel card)
    {
        return card.TargetType switch
        {
            TargetType.Self => Owner.Creature,
            TargetType.AnyEnemy or TargetType.RandomEnemy => LowestHealthEnemy(),
            TargetType.AnyPlayer or TargetType.AnyAlly => DefaultAlly(card),
            _ => null
        };
    }

    private Creature LowestHealthEnemy()
    {
        return CombatState.HittableEnemies
            .OrderBy(enemy => enemy.CurrentHp)
            .FirstOrDefault();
    }

    private Creature DefaultAlly(CardModel card)
    {
        if (card.CanPlayTargeting(Owner.Creature))
        {
            return Owner.Creature;
        }

        return CombatState.PlayerCreatures
            .Where(creature => creature != Owner.Creature && !creature.IsDead)
            .FirstOrDefault(card.CanPlayTargeting);
    }

    private static bool RequiresTarget(CardModel card)
    {
        return card.TargetType is
            TargetType.Self or
            TargetType.AnyEnemy or
            TargetType.RandomEnemy or
            TargetType.AnyPlayer or
            TargetType.AnyAlly;
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
