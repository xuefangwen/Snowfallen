using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Cards;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Mechanics;

public static class EmberMechanics
{
    public static void ApplyMaterialBonuses(IEmberCard subject, IEmberCard material)
    {
        if (material.Card is not Annotation annotation || subject.Card.Owner != annotation.Owner)
            return;
        // Apply on the same card instance, including the stronger-material branch.
        if (subject.Card.Enchantment is not SnowfallenMod.SnowfallenCode.Enchantments.AnnotationMark)
        {
            if (subject.Card.Enchantment is not null) CardCmd.ClearEnchantment(subject.Card);
            CardCmd.Enchant<SnowfallenMod.SnowfallenCode.Enchantments.AnnotationMark>(subject.Card, 1m);
        }
        if (subject.Card.Pile?.Type == PileType.Discard &&
            subject.Card.Enchantment is SnowfallenMod.SnowfallenCode.Enchantments.AnnotationMark mark)
            mark.PendingReturn = true;
        subject.AddReplayBonus(annotation.DynamicVars["MaterialReplay"].IntValue);
    }

    public static async Task<EmberTrackerPower> EnsureTracker(
        PlayerChoiceContext choiceContext,
        Player owner,
        ICombatState combatState,
        CardModel source)
    {
        EmberTrackerPower tracker = owner.Creature.Powers.OfType<EmberTrackerPower>().FirstOrDefault();
        if (tracker is null)
        {
            await PowerCmd.Apply<EmberTrackerPower>(
                choiceContext,
                owner.Creature,
                1m,
                owner.Creature,
                source);
            tracker = owner.Creature.Powers.OfType<EmberTrackerPower>().First();
        }

        return tracker;
    }

    public static List<Ember> Create(
        Player owner,
        ICombatState combatState,
        int count,
        decimal permanentDamageBonus = 0m,
        decimal permanentBlockBonus = 0m,
        decimal temporaryDamageBonus = 0m,
        bool aoeThisTurn = false,
        Action<Ember> configure = null)
    {
        List<Ember> embers = [];
        for (int i = 0; i < count; i++)
        {
            Ember ember = ((CombatState)combatState).CreateCard<Ember>(owner);
            ember.AddPermanentDamageBonus(permanentDamageBonus);
            ember.AddPermanentBlockBonus(permanentBlockBonus);
            ember.AddTemporaryDamageBonus(temporaryDamageBonus);
            if (aoeThisTurn)
            {
                ember.MakeAoEThisTurn();
            }
            configure?.Invoke(ember);
            embers.Add(ember);
        }

        return embers;
    }

    public static async Task<List<Ember>> CreateInHand(
        Player owner,
        ICombatState combatState,
        int count,
        decimal permanentDamageBonus = 0m,
        decimal permanentBlockBonus = 0m,
        decimal temporaryDamageBonus = 0m,
        bool aoeThisTurn = false,
        Action<Ember> configure = null)
    {
        return await CreateInPile(
            owner,
            combatState,
            count,
            PileType.Hand,
            CardPilePosition.Bottom,
            permanentDamageBonus,
            permanentBlockBonus,
            temporaryDamageBonus,
            aoeThisTurn,
            configure);
    }

    public static async Task<List<Ember>> CreateInPile(
        Player owner,
        ICombatState combatState,
        int count,
        PileType pileType,
        CardPilePosition position = CardPilePosition.Bottom,
        decimal permanentDamageBonus = 0m,
        decimal permanentBlockBonus = 0m,
        decimal temporaryDamageBonus = 0m,
        bool aoeThisTurn = false,
        Action<Ember> configure = null)
    {
        List<Ember> embers = Create(
            owner,
            combatState,
            count,
            permanentDamageBonus,
            permanentBlockBonus,
            temporaryDamageBonus,
            aoeThisTurn,
            configure);
        EmberTrackerPower tracker = null;
        if (embers.Count > 0)
        {
            tracker = await EnsureTracker(
                new BlockingPlayerChoiceContext(),
                owner,
                combatState,
                embers[0]);
            foreach (Ember ember in embers)
            {
                tracker.RegisterNewActiveEmber(ember);
            }
        }
        await CardPileCmd.AddGeneratedCardsToCombat(
            embers,
            pileType,
            owner,
            position);
        return embers;
    }

    public static int Count(Player owner, PileType pileType)
    {
        return pileType.GetPile(owner).Cards.OfType<IEmberCard>().Count();
    }
}
