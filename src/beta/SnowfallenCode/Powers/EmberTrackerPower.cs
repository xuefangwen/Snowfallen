using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Cards;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Powers;

public class EmberTrackerPower : SnowfallenPower
{
    public int PlayedThisTurn { get; private set; }
    public int GeneratedThisCombat { get; private set; }
    public int ExhaustedThisCombat { get; private set; }
    public decimal GlobalPlayGrowthBonus { get; private set; }

    private long _nextMergeOrder;
    private readonly HashSet<IEmberCard> _fusionReservations =
        new(ReferenceEqualityComparer.Instance);
    private readonly Queue<Ember> _pendingReturnToHand = new();
    private readonly HashSet<Ember> _queuedReturnToHand =
        new(ReferenceEqualityComparer.Instance);
    private bool _isResolvingEndTurnFusion;
    private bool _fusionResolvedThisTurn;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    public void RecordPlayed()
    {
        PlayedThisTurn++;
        InvokeDisplayAmountChanged();
    }

    public void RecordGenerated()
    {
        GeneratedThisCombat++;
        InvokeDisplayAmountChanged();
    }

    public void RecordExhausted()
    {
        ExhaustedThisCombat++;
        InvokeDisplayAmountChanged();
    }

    public void AddGlobalPlayGrowthBonus(decimal amount)
    {
        GlobalPlayGrowthBonus += Math.Max(0m, amount);
        InvokeDisplayAmountChanged();
    }

    public void RegisterNewActiveEmber(IEmberCard ember)
    {
        if (Owner.Player is null || ember.Card.Owner != Owner.Player)
        {
            return;
        }

        ember.AssignMergeOrder(NextMergeOrder());
    }

    public void ReserveFromFusion(IEnumerable<IEmberCard> embers)
    {
        foreach (IEmberCard ember in embers)
        {
            _fusionReservations.Add(ember);
        }
    }

    public void ReleaseFromFusion(IEmberCard ember)
    {
        _fusionReservations.Remove(ember);
    }

    public void ReleaseFromFusion(IEnumerable<IEmberCard> embers)
    {
        foreach (IEmberCard ember in embers)
        {
            _fusionReservations.Remove(ember);
        }
    }

    public void QueueReturnToHand(Ember ember)
    {
        if (_queuedReturnToHand.Add(ember))
        {
            _pendingReturnToHand.Enqueue(ember);
        }
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await FlushReturnsToHand();
    }

    public override async Task AfterCardDiscarded(
        PlayerChoiceContext choiceContext,
        CardModel card)
    {
        await FlushReturnsToHand();
    }

    public override async Task AfterFlush(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyCollection<CardModel> flushedCards,
        IReadOnlyCollection<CardModel> retainedCards)
    {
        if (player != Owner.Player)
        {
            return;
        }

        await FlushReturnsToHand();
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> creatures)
    {
        if (side != Owner.Side)
        {
            return;
        }

        await FlushReturnsToHand();
        await ResolveEndTurnFusion(choiceContext);
        await FlushReturnsToHand();
    }

    private async Task ResolveEndTurnFusion(
        PlayerChoiceContext choiceContext)
    {
        if (_isResolvingEndTurnFusion ||
            _fusionResolvedThisTurn ||
            Owner.Player is null)
        {
            return;
        }

        List<IEmberCard> discardEmbers = DiscardEmbers();
        if (discardEmbers.Count < 2)
        {
            _fusionResolvedThisTurn = true;
            return;
        }

        _isResolvingEndTurnFusion = true;
        _fusionResolvedThisTurn = true;
        IEmberCard subject = null;
        try
        {
            List<IEmberCard> subjectCandidates = discardEmbers
                .Where(ember => !_fusionReservations.Contains(ember))
                .ToList();
            if (subjectCandidates.Count == 0)
            {
                return;
            }

            subject = (await CardSelectCmd.FromSimpleGrid(
                    choiceContext,
                    subjectCandidates.Select(candidate => candidate.Card).ToList(),
                    Owner.Player,
                    new CardSelectorPrefs(
                        new LocString(
                            "cards",
                            "SNOWFALLENMOD-EMBER.fusionSubjectPrompt"),
                        1,
                        1)))
                .OfType<IEmberCard>()
                .FirstOrDefault();
            if (subject is null ||
                !PileType.Discard.GetPile(Owner.Player).Cards
                    .Contains(subject.Card))
            {
                return;
            }

            subject.SetFusionSubject(true);

            discardEmbers = DiscardEmbers();
            int maximumMaterials = Math.Max(0, discardEmbers.Count - 1);
            List<IEmberCard> materialCandidates = discardEmbers
                .Where(ember =>
                    !ReferenceEquals(ember, subject) &&
                    !_fusionReservations.Contains(ember))
                .ToList();
            maximumMaterials = Math.Min(
                maximumMaterials,
                materialCandidates.Count);
            if (maximumMaterials <= 0)
            {
                return;
            }

            List<IEmberCard> selectedMaterials =
                (await CardSelectCmd.FromSimpleGrid(
                    choiceContext,
                    materialCandidates.Select(candidate => candidate.Card).ToList(),
                    Owner.Player,
                    new CardSelectorPrefs(
                        new LocString(
                            "cards",
                            "SNOWFALLENMOD-EMBER.fusionMaterialPrompt"),
                        0,
                        maximumMaterials)))
                .OfType<IEmberCard>()
                .ToList();

            foreach (IEmberCard material in selectedMaterials)
            {
                CardPile discardPile =
                    PileType.Discard.GetPile(Owner.Player);
                if (!discardPile.Cards.Contains(subject.Card) ||
                    !discardPile.Cards.Contains(material.Card) ||
                    ReferenceEquals(subject, material))
                {
                    continue;
                }

                await CardCmd.Exhaust(choiceContext, material.Card);
                await FlushReturnsToHand();

                // Only an Ember that actually reaches the exhaust pile is
                // fused. Effects that prevent its exhaust also prevent it
                // from becoming material.
                if (PileType.Exhaust.GetPile(Owner.Player).Cards
                    .Contains(material.Card))
                {
                    subject.Absorb(material);
                }
            }
        }
        finally
        {
            subject?.SetFusionSubject(false);
            _isResolvingEndTurnFusion = false;
        }
    }

    private async Task FlushReturnsToHand()
    {
        if (Owner.Player is null)
        {
            return;
        }

        while (_pendingReturnToHand.Count > 0)
        {
            Ember ember = _pendingReturnToHand.Dequeue();
            _queuedReturnToHand.Remove(ember);
            if (PileType.Exhaust.GetPile(Owner.Player).Cards.Contains(ember))
            {
                await CardPileCmd.Add(
                    ember,
                    PileType.Hand,
                    CardPilePosition.Bottom);
            }
        }
    }

    private List<IEmberCard> DiscardEmbers()
    {
        if (Owner.Player is null)
        {
            return [];
        }

        return PileType.Discard.GetPile(Owner.Player).Cards
            .OfType<IEmberCard>()
            .ToList();
    }

    private List<IEmberCard> ActiveEmbers()
    {
        if (Owner.Player is null)
        {
            return [];
        }

        return
        [
            .. PileType.Discard.GetPile(Owner.Player).Cards.OfType<IEmberCard>(),
            .. PileType.Hand.GetPile(Owner.Player).Cards.OfType<IEmberCard>(),
            .. PileType.Draw.GetPile(Owner.Player).Cards.OfType<IEmberCard>()
        ];
    }

    private long NextMergeOrder()
    {
        long greatestExistingOrder = ActiveEmbers()
            .Select(ember => ember.MergeOrder)
            .DefaultIfEmpty(0L)
            .Max();
        _nextMergeOrder = Math.Max(_nextMergeOrder, greatestExistingOrder) + 1L;
        return _nextMergeOrder;
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (!creatures.Contains(Owner) || Owner.Player is null)
        {
            return;
        }

        PlayedThisTurn = 0;
        _fusionResolvedThisTurn = false;
        List<IEmberCard> returning = PileType.Exhaust.GetPile(Owner.Player).Cards
            .OfType<IEmberCard>()
            .Where(ember => ember.ReturnNextTurn)
            .ToList();
        foreach (IEmberCard ember in returning)
        {
            ember.ClearReturnNextTurn();
        }

        if (returning.Count > 0)
        {
            foreach (IEmberCard ember in returning)
            {
                RegisterNewActiveEmber(ember);
            }
            await CardPileCmd.Add(
                returning.Select(ember => ember.Card),
                PileType.Hand);
        }

        InvokeDisplayAmountChanged();
    }
}

