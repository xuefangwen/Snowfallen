using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class SilentPalace() : SnowfallenCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WaterBreathPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(2), new PowerVar<WaterBreathPower>("WaterPerCard", 4m)];

    // Fixed water-related pool: includes consumers and water-scaling cards.
    internal static bool ConsumesWaterBreath(CardModel card) => card is
        Contents or FrostForestDrunk or SpringSnow or WatersidePavilion or
        LingeringEmber or EpochOfAnnihilation or SongOfFloatingClouds or
        RaiseGoblets or Rubbing;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var candidates = PileType.Discard.GetPile(Owner).Cards
            .Where(ConsumesWaterBreath).ToList();
        if (candidates.Count == 0) return;
        var selected = (await CardSelectCmd.FromSimpleGrid(choiceContext, candidates, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, Math.Min(DynamicVars.Cards.IntValue, candidates.Count))
            { RequireManualConfirmation = true })).ToList();
        await PlaySelected(choiceContext, selected);
    }

    internal async Task PlaySelected(
        PlayerChoiceContext choiceContext, IEnumerable<CardModel> selected)
    {
        var discard = PileType.Discard.GetPile(Owner);
        var play = PileType.Play.GetPile(Owner);
        // Keep the synchronized selection order and reserve only our own cards.
        var reserved = selected.Where(card => card.Owner == Owner &&
            card.Pile == discard && !card.HasBeenRemovedFromState).Distinct().ToList();
        if (reserved.Count == 0 || !CanContinuePlaying()) return;

        try
        {
            // Reserve the whole batch before any card can draw or shuffle.
            await CardPileCmd.Add(reserved, PileType.Play);
            foreach (CardModel card in reserved)
            {
                if (!CanContinuePlaying()) break;
                if (card.Owner != Owner || card.HasBeenRemovedFromState ||
                    card.Pile != play) continue;
                await WaterBreathMechanics.Gain(choiceContext, Owner.Creature,
                    DynamicVars["WaterPerCard"].BaseValue, Owner.Creature, this);
                if (!CanContinuePlaying()) break;
                // AutoPlay retains the game's prohibitions and result-pile rules.
                await CardCmd.AutoPlay(choiceContext, card, null);
            }
        }
        finally
        {
            // Do not strand unplayed reservations when an effect stops the batch.
            // Dead players / ending combats use the game's combat-pile teardown;
            // CardPileCmd.Add deliberately refuses moves in those states.
            if (!Owner.Creature.IsDead && !CombatManager.Instance.IsOverOrEnding &&
                Owner.PlayerCombatState is not null)
            {
                var remaining = reserved.Where(card => card.Owner == Owner &&
                    !card.HasBeenRemovedFromState && card.Pile == play).ToList();
                await CardPileCmd.Add(remaining, PileType.Discard);
            }
        }
    }

    private bool CanContinuePlaying() =>
        !Owner.Creature.IsDead && !CombatManager.Instance.IsOverOrEnding &&
        Owner.Creature.CombatState?.HittableEnemies.Count > 0;

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
        DynamicVars["WaterPerCard"].UpgradeValueBy(2m);
    }
}
