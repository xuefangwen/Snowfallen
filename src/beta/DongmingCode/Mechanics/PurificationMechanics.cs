using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.DongmingCode.Cards;
using SnowfallenMod.DongmingCode.Enchantments;
using SnowfallenMod.DongmingCode.Powers;
using SnowfallenMod.SnowfallenCode.Cards;
using SnowfallenMod.SnowfallenCode.Extensions;
using VanillaVoid = MegaCrit.Sts2.Core.Models.Cards.Void;

namespace SnowfallenMod.DongmingCode.Mechanics;

public static class PurificationMechanics
{
    private static readonly HashSet<CardModel> TransformingCards =
        new(ReferenceEqualityComparer.Instance);
    private static readonly HashSet<CardModel> ExhaustingFromHand =
        new(ReferenceEqualityComparer.Instance);

    public static bool IsTransformable(CardModel card) =>
        card is IMoonPhaseCard or PurifiedVanillaCard ||
        card is Day0Curse or RottenWood ||
        IsSupportedVanillaCard(card) ||
        card.Enchantment is DongmingCurseEnchantment &&
        card.Type is CardType.Attack or CardType.Skill or CardType.Power;

    public static bool IsPurifiedCard(CardModel card) =>
        card is PurifiedVanillaCard;

    public static bool IsGeneratedPurifiedCard(CardModel card) =>
        card is PurifiedVanillaCard;

    public static IHoverTip GetOriginalHoverTip(PurifiedVanillaCard card) => card switch
    {
        PurifiedAscendersBane => HoverTipFactory.FromCard<AscendersBane>(),
        PurifiedBadLuck => HoverTipFactory.FromCard<BadLuck>(),
        PurifiedBeckon => HoverTipFactory.FromCard<Beckon>(),
        PurifiedBurn => HoverTipFactory.FromCard<Burn>(),
        PurifiedClumsy => HoverTipFactory.FromCard<Clumsy>(),
        PurifiedCurseOfTheBell => HoverTipFactory.FromCard<CurseOfTheBell>(),
        PurifiedDazed => HoverTipFactory.FromCard<Dazed>(),
        PurifiedDebris => HoverTipFactory.FromCard<Debris>(),
        PurifiedDebt => HoverTipFactory.FromCard<Debt>(),
        PurifiedDecay => HoverTipFactory.FromCard<Decay>(),
        PurifiedDoubt => HoverTipFactory.FromCard<Doubt>(),
        PurifiedEnthralled => HoverTipFactory.FromCard<Enthralled>(),
        PurifiedFolly => HoverTipFactory.FromCard<Folly>(),
        PurifiedFranticEscape => HoverTipFactory.FromCard<FranticEscape>(),
        PurifiedGreed => HoverTipFactory.FromCard<Greed>(),
        PurifiedGuilty => HoverTipFactory.FromCard<Guilty>(),
        PurifiedInfection => HoverTipFactory.FromCard<Infection>(),
        PurifiedInjury => HoverTipFactory.FromCard<Injury>(),
        PurifiedNormality => HoverTipFactory.FromCard<Normality>(),
        PurifiedPoorSleep => HoverTipFactory.FromCard<PoorSleep>(),
        PurifiedRegret => HoverTipFactory.FromCard<Regret>(),
        PurifiedShame => HoverTipFactory.FromCard<Shame>(),
        PurifiedSlimed => HoverTipFactory.FromCard<Slimed>(),
        PurifiedSoot => HoverTipFactory.FromCard<Soot>(),
        PurifiedSporeMind => HoverTipFactory.FromCard<SporeMind>(),
        PurifiedToxic => HoverTipFactory.FromCard<Toxic>(),
        PurifiedVoid => HoverTipFactory.FromCard<VanillaVoid>(),
        PurifiedWither => HoverTipFactory.FromCard<Wither>(),
        PurifiedWound => HoverTipFactory.FromCard<Wound>(),
        PurifiedWrithe => HoverTipFactory.FromCard<Writhe>(),
        _ => throw new InvalidOperationException($"No original hover tip for {card.GetType().FullName}.")
    };

    public static IHoverTip GetPurifiedHoverTip(CardModel card) => card switch
    {
        AscendersBane => HoverTipFactory.FromCard<PurifiedAscendersBane>(),
        BadLuck => HoverTipFactory.FromCard<PurifiedBadLuck>(),
        Beckon => HoverTipFactory.FromCard<PurifiedBeckon>(),
        Burn => HoverTipFactory.FromCard<PurifiedBurn>(),
        Clumsy => HoverTipFactory.FromCard<PurifiedClumsy>(),
        CurseOfTheBell => HoverTipFactory.FromCard<PurifiedCurseOfTheBell>(),
        Dazed => HoverTipFactory.FromCard<PurifiedDazed>(),
        Debris => HoverTipFactory.FromCard<PurifiedDebris>(),
        Debt => HoverTipFactory.FromCard<PurifiedDebt>(),
        Decay => HoverTipFactory.FromCard<PurifiedDecay>(),
        Doubt => HoverTipFactory.FromCard<PurifiedDoubt>(),
        Enthralled => HoverTipFactory.FromCard<PurifiedEnthralled>(),
        Folly => HoverTipFactory.FromCard<PurifiedFolly>(),
        FranticEscape => HoverTipFactory.FromCard<PurifiedFranticEscape>(),
        Greed => HoverTipFactory.FromCard<PurifiedGreed>(),
        Guilty => HoverTipFactory.FromCard<PurifiedGuilty>(),
        Infection => HoverTipFactory.FromCard<PurifiedInfection>(),
        Injury => HoverTipFactory.FromCard<PurifiedInjury>(),
        Normality => HoverTipFactory.FromCard<PurifiedNormality>(),
        PoorSleep => HoverTipFactory.FromCard<PurifiedPoorSleep>(),
        Regret => HoverTipFactory.FromCard<PurifiedRegret>(),
        Shame => HoverTipFactory.FromCard<PurifiedShame>(),
        Slimed => HoverTipFactory.FromCard<PurifiedSlimed>(),
        Soot => HoverTipFactory.FromCard<PurifiedSoot>(),
        SporeMind => HoverTipFactory.FromCard<PurifiedSporeMind>(),
        Toxic => HoverTipFactory.FromCard<PurifiedToxic>(),
        VanillaVoid => HoverTipFactory.FromCard<PurifiedVoid>(),
        RottenWood => HoverTipFactory.FromCard<PurifiedWither>(),
        Wither => HoverTipFactory.FromCard<PurifiedWither>(),
        Wound => HoverTipFactory.FromCard<PurifiedWound>(),
        Writhe => HoverTipFactory.FromCard<PurifiedWrithe>(),
        _ => null
    };

    public static bool IsSupportedVanillaCard(CardModel card) => card is
        AscendersBane or BadLuck or Beckon or Burn or Clumsy or
        CurseOfTheBell or Dazed or Debris or Debt or Decay or Doubt or
        Enthralled or Folly or FranticEscape or Greed or Guilty or Infection or
        Injury or Normality or PoorSleep or Regret or Shame or Slimed or Soot or
        SporeMind or Toxic or VanillaVoid or Wither or Wound or Writhe;

    public static async Task Transform(
        PlayerChoiceContext choiceContext,
        CardModel card)
    {
        Player owner = card.Owner;
        bool transformed = false;

        if (card.Enchantment is DongmingCurseEnchantment &&
            card.Type is CardType.Attack or CardType.Skill or CardType.Power)
        {
            CardCmd.ClearEnchantment(card);
            MoonPhaseMechanics.RefreshCardVisuals(card);
            transformed = true;
        }
        else if (card is IMoonPhaseCard moonPhaseCard)
        {
            moonPhaseCard.ToggleMoonPhase();
            transformed = true;
        }
        else if (card is PurifiedVanillaCard purified)
        {
            transformed = await ReplaceWithOriginal(choiceContext, purified);
        }
        else if (card is RottenWood wood)
        {
            transformed = await ReplaceRottenWood(choiceContext, wood);
        }
        else if (card is Day0Curse day0Curse)
        {
            transformed = await ReplaceDay0Curse(choiceContext, day0Curse);
        }
        else if (IsSupportedVanillaCard(card))
        {
            transformed = await ReplaceWithPurified(choiceContext, card);
        }

        if (!transformed)
        {
            return;
        }

        // Every successful transformation emits exactly one combat flip notification,
        // including enchantment removal and DAY0 conversions.
        await MoonPhaseMechanics.NotifyCombatPhaseFlip(card, choiceContext);
        await MoonlightMechanics.Gain(owner, 1m);
        await PowerCmd.Apply<DoomPower>(
            choiceContext,
            owner.Creature,
            3m,
            owner.Creature,
            card);
    }

    public static async Task TransformTwice(
        PlayerChoiceContext choiceContext,
        CardModel card)
    {
        Player owner = card.Owner;
        HashSet<CardModel> before = new(
            PileType.Hand.GetPile(owner).Cards,
            ReferenceEqualityComparer.Instance);
        await Transform(choiceContext, card);

        CardModel second = card.Pile?.Type == PileType.Hand
            ? card
            : PileType.Hand.GetPile(owner).Cards
                .FirstOrDefault(candidate => !before.Contains(candidate));
        if (second is not null && IsTransformable(second))
            await Transform(choiceContext, second);
    }

    public static async Task SelectAndTransform(
        PlayerChoiceContext choiceContext,
        Player owner,
        CardModel source,
        int maximum,
        bool optional)
    {
        List<CardModel> candidates = PileType.Hand.GetPile(owner).Cards
            .Where(card => !ReferenceEquals(card, source))
            .Where(IsTransformable)
            .ToList();
        int max = Math.Min(maximum, candidates.Count);
        if (max <= 0)
        {
            return;
        }

        int minimum = optional ? 0 : max;
        CardSelectorPrefs prefs = new(
            new LocString("cards", $"{source.Id.Entry}.selectionScreenPrompt"),
            minimum,
            max)
        {
            Cancelable = optional,
            RequireManualConfirmation = true
        };
        IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
            choiceContext,
            owner,
            prefs,
            candidates.Contains,
            source);
        foreach (CardModel selectedCard in selected.Distinct().ToList())
        {
            await Transform(choiceContext, selectedCard);
        }
    }

    public static void MarkExhaustingFromHand(CardModel card)
    {
        if (card is PurifiedVanillaCard && card.Pile?.Type == PileType.Hand)
        {
            ExhaustingFromHand.Add(card);
        }
    }

    public static bool ConsumeExhaustedFromHand(CardModel card) =>
        ExhaustingFromHand.Remove(card);

    public static bool ConsumeTransforming(CardModel card) =>
        TransformingCards.Remove(card);

    private static async Task<bool> ReplaceWithPurified(
        PlayerChoiceContext choiceContext,
        CardModel original)
    {
        Player owner = original.Owner;
        if (original.Enchantment is DongmingCurseEnchantment)
        {
            CardCmd.ClearEnchantment(original);
        }

        TransformingCards.Add(original);
        try
        {
            await CardCmd.Exhaust(choiceContext, original, false, false);
        }
        finally
        {
            TransformingCards.Remove(original);
        }

        if (original.Pile?.Type != PileType.Exhaust)
        {
            return false;
        }

        PurifiedVanillaCard generated = CreatePurified(original, owner);
        await CardPileCmd.AddGeneratedCardToCombat(
            generated,
            PileType.Hand,
            owner,
            CardPilePosition.Bottom);
        await generated.ResolvePurified(choiceContext);
        return true;
    }

    /// <summary>
    /// 东铭对【DAY0的诅咒】的转换：把这张诅咒烧掉（消耗），
    /// 并在手牌生成一张未升级的【我欲成仙】。
    /// 与净化原版诅咒同一套口径（消耗 → 生成到手牌），
    /// 月华/灾厄奖励由 Transform 统一结算。
    /// </summary>
    private static async Task<bool> ReplaceRottenWood(PlayerChoiceContext context, RottenWood wood)
    {
        Player owner = wood.Owner;
        TransformingCards.Add(wood);
        try { await CardCmd.Exhaust(context, wood, false, false); }
        finally { TransformingCards.Remove(wood); }
        if (wood.Pile?.Type != PileType.Exhaust) return false;
        var generated = ((CombatState)owner.Creature.CombatState).CreateCard<PurifiedWither>(owner);
        generated.RottenWoodEthereal = true;
        generated.HealingAmount = 5;
        await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, owner, CardPilePosition.Bottom);
        await generated.ResolvePurified(context);
        return true;
    }

    private static async Task<bool> ReplaceDay0Curse(
        PlayerChoiceContext choiceContext,
        Day0Curse curse)
    {
        Player owner = curse.Owner;

        TransformingCards.Add(curse);
        try
        {
            await CardCmd.Exhaust(choiceContext, curse, false, false);
        }
        finally
        {
            TransformingCards.Remove(curse);
        }

        if (curse.Pile?.Type != PileType.Exhaust)
        {
            return false;
        }

        CombatState combat = (CombatState)owner.Creature.CombatState;
        CardModel generated = combat.CreateCard<WuYuChengXian>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(
            generated,
            PileType.Hand,
            owner,
            CardPilePosition.Bottom);
        return true;
    }

    private static async Task<bool> ReplaceWithOriginal(
        PlayerChoiceContext choiceContext,
        PurifiedVanillaCard purified)
    {
        Player owner = purified.Owner;
        TransformingCards.Add(purified);
        try
        {
            await CardCmd.Exhaust(choiceContext, purified, false, false);
        }
        finally
        {
            TransformingCards.Remove(purified);
            ExhaustingFromHand.Remove(purified);
        }

        if (purified.Pile?.Type != PileType.Exhaust)
        {
            return false;
        }

        await PurifiedVanillaEffects.OnReversed(choiceContext, purified);

        CardModel generated = CreateOriginal(purified, owner);
        await CardPileCmd.AddGeneratedCardToCombat(
            generated,
            PileType.Hand,
            owner,
            CardPilePosition.Bottom);
        return true;
    }

    private static PurifiedVanillaCard CreatePurified(
        CardModel original,
        Player owner)
    {
        CombatState combat = (CombatState)owner.Creature.CombatState;
        return original switch
        {
            AscendersBane => combat.CreateCard<PurifiedAscendersBane>(owner),
            BadLuck => combat.CreateCard<PurifiedBadLuck>(owner),
            Beckon => combat.CreateCard<PurifiedBeckon>(owner),
            Burn => combat.CreateCard<PurifiedBurn>(owner),
            Clumsy => combat.CreateCard<PurifiedClumsy>(owner),
            CurseOfTheBell => combat.CreateCard<PurifiedCurseOfTheBell>(owner),
            Dazed => combat.CreateCard<PurifiedDazed>(owner),
            Debris => combat.CreateCard<PurifiedDebris>(owner),
            Debt => combat.CreateCard<PurifiedDebt>(owner),
            Decay => combat.CreateCard<PurifiedDecay>(owner),
            Doubt => combat.CreateCard<PurifiedDoubt>(owner),
            Enthralled => combat.CreateCard<PurifiedEnthralled>(owner),
            Folly => combat.CreateCard<PurifiedFolly>(owner),
            FranticEscape => combat.CreateCard<PurifiedFranticEscape>(owner),
            Greed => combat.CreateCard<PurifiedGreed>(owner),
            Guilty => combat.CreateCard<PurifiedGuilty>(owner),
            Infection => combat.CreateCard<PurifiedInfection>(owner),
            Injury => combat.CreateCard<PurifiedInjury>(owner),
            Normality => combat.CreateCard<PurifiedNormality>(owner),
            PoorSleep => combat.CreateCard<PurifiedPoorSleep>(owner),
            Regret => combat.CreateCard<PurifiedRegret>(owner),
            Shame => combat.CreateCard<PurifiedShame>(owner),
            Slimed => combat.CreateCard<PurifiedSlimed>(owner),
            Soot => combat.CreateCard<PurifiedSoot>(owner),
            SporeMind => combat.CreateCard<PurifiedSporeMind>(owner),
            Toxic => combat.CreateCard<PurifiedToxic>(owner),
            VanillaVoid => combat.CreateCard<PurifiedVoid>(owner),
            Wither wither => CreatePurifiedWither(combat, owner, wither),
            Wound => combat.CreateCard<PurifiedWound>(owner),
            Writhe => combat.CreateCard<PurifiedWrithe>(owner),
            _ => throw new InvalidOperationException(
                $"No purified card is mapped for {original.GetType().FullName}.")
        };
    }

    private static PurifiedWither CreatePurifiedWither(CombatState combat, Player owner, Wither original)
    {
        PurifiedWither generated = combat.CreateCard<PurifiedWither>(owner);
        generated.HealingAmount = (int)original.DynamicVars.Damage.BaseValue;
        return generated;
    }

    private static Wither CreateWither(CombatState combat, Player owner, PurifiedWither purified)
    {
        Wither generated = combat.CreateCard<Wither>(owner);
        // Combat generation can already apply the Aeonglass upgrade count.
        if (!purified.RottenWoodEthereal)
            generated.DynamicVars.Damage.BaseValue = purified.HealingAmount;
        return generated;
    }

    private static CardModel CreateOriginal(
        PurifiedVanillaCard purified,
        Player owner)
    {
        CombatState combat = (CombatState)owner.Creature.CombatState;
        return purified switch
        {
            PurifiedAscendersBane => combat.CreateCard<AscendersBane>(owner),
            PurifiedBadLuck => combat.CreateCard<BadLuck>(owner),
            PurifiedBeckon => combat.CreateCard<Beckon>(owner),
            PurifiedBurn => combat.CreateCard<Burn>(owner),
            PurifiedClumsy => combat.CreateCard<Clumsy>(owner),
            PurifiedCurseOfTheBell => combat.CreateCard<CurseOfTheBell>(owner),
            PurifiedDazed => combat.CreateCard<Dazed>(owner),
            PurifiedDebris => combat.CreateCard<Debris>(owner),
            PurifiedDebt => combat.CreateCard<Debt>(owner),
            PurifiedDecay => combat.CreateCard<Decay>(owner),
            PurifiedDoubt => combat.CreateCard<Doubt>(owner),
            PurifiedEnthralled => combat.CreateCard<Enthralled>(owner),
            PurifiedFolly => combat.CreateCard<Folly>(owner),
            PurifiedFranticEscape => combat.CreateCard<FranticEscape>(owner),
            PurifiedGreed => combat.CreateCard<Greed>(owner),
            PurifiedGuilty => combat.CreateCard<Guilty>(owner),
            PurifiedInfection => combat.CreateCard<Infection>(owner),
            PurifiedInjury => combat.CreateCard<Injury>(owner),
            PurifiedNormality => combat.CreateCard<Normality>(owner),
            PurifiedPoorSleep => combat.CreateCard<PoorSleep>(owner),
            PurifiedRegret => combat.CreateCard<Regret>(owner),
            PurifiedShame => combat.CreateCard<Shame>(owner),
            PurifiedSlimed => combat.CreateCard<Slimed>(owner),
            PurifiedSoot => combat.CreateCard<Soot>(owner),
            PurifiedSporeMind => combat.CreateCard<SporeMind>(owner),
            PurifiedToxic => combat.CreateCard<Toxic>(owner),
            PurifiedVoid => combat.CreateCard<VanillaVoid>(owner),
            PurifiedWither wither => CreateWither(combat, owner, wither),
            PurifiedWound => combat.CreateCard<Wound>(owner),
            PurifiedWrithe => combat.CreateCard<Writhe>(owner),
            _ => throw new InvalidOperationException(
                $"No original card is mapped for {purified.GetType().FullName}.")
        };
    }
}

internal static class PurifiedVanillaEffects
{
    public static async Task OnReversed(
        PlayerChoiceContext choiceContext,
        PurifiedVanillaCard card)
    {
        switch (card)
        {
            case PurifiedDebt debt:
                await RemoveInterestReward(
                    choiceContext,
                    debt,
                    debt.InterestReward > 0
                        ? debt.InterestReward
                        : 20m + debt.DiscardCount * 5m);
                break;
            case PurifiedWrithe writhe:
                writhe.PendingNextTurnTriggers = 0;
                await ResolveWritheEffect(choiceContext, card);
                break;
            case PurifiedPoorSleep poorSleep:
                poorSleep.Pending = 0;
                poorSleep.DueNextTurn = 0;
                poorSleep.DueFollowingTurn = 0;
                await SyncAnMianPower(choiceContext, card.Owner);
                break;
        }
    }

    public static async Task OnPurified(
        PlayerChoiceContext choiceContext,
        PurifiedVanillaCard card)
    {
        Player owner = card.Owner;
        switch (card)
        {
            case PurifiedVoid:
                await PlayerCmd.GainEnergy(2m, owner);
                await MoonlightMechanics.Gain(owner, 3m);
                break;
            case PurifiedAscendersBane:
                await ApplySoar(choiceContext, card, 2m);
                await PowerCmd.Apply<WeakPower>(
                    choiceContext, owner.Creature, 2m, owner.Creature, card);
                await MoonlightMechanics.Gain(owner, 2m);
                break;
            case PurifiedCurseOfTheBell:
                await GrantVigorToAllies(choiceContext, owner, owner.Relics.Count);
                break;
            case PurifiedClumsy:
                await PowerCmd.Apply<CleverThoughtPower>(
                    choiceContext, owner.Creature, 1m, owner.Creature, card);
                break;
            case PurifiedGreed:
                await PlayerCmd.GainGold(20m, owner, false);
                await MoonlightMechanics.Gain(owner, 2m);
                await PowerCmd.Apply<FrailPower>(
                    choiceContext, owner.Creature, 1m, owner.Creature, card);
                break;
            case PurifiedGuilty:
                await CreatureCmd.Damage(
                    choiceContext,
                    owner.Creature,
                    3m,
                    DamageProps.cardHpLoss,
                    card,
                    null);
                await PlayerCmd.GainEnergy(1m, owner);
                await MoonlightMechanics.Gain(owner, 1m);
                await CardPileCmd.Draw(choiceContext, 3, owner, false);
                break;
            case PurifiedInjury:
                await CreatureCmd.Heal(owner.Creature, 6m);
                await MoonlightMechanics.Gain(owner, 2m);
                break;
            case PurifiedPoorSleep poorSleep:
                AddPoorSleepPending(poorSleep);
                await SyncAnMianPower(choiceContext, owner);
                break;
            case PurifiedWrithe:
                await ResolveWritheEffect(choiceContext, card);
                break;
            case PurifiedSoot:
                await EnchantRandomAttack(card);
                break;
            case PurifiedWound:
                await PowerCmd.Apply<RegenPower>(
                    choiceContext, owner.Creature, 3m, owner.Creature, card);
                break;
            case PurifiedDebt debt:
                await AddDebtInterestReward(choiceContext, debt, 20);
                break;
            case PurifiedFolly:
                await ResolveFolly(choiceContext, card);
                break;
            case PurifiedNormality:
                await MoonlightMechanics.Gain(owner, 2m);
                await PowerCmd.Apply<DoomPower>(
                    choiceContext, owner.Creature, 5m, owner.Creature, card);
                await CardPileCmd.Draw(choiceContext, 3, owner);
                await PowerCmd.Apply<DrawCardsNextTurnPower>(
                    choiceContext, owner.Creature, 3m, owner.Creature, card);
                break;
        }
    }

    public static async Task OnPlay(
        PlayerChoiceContext choiceContext,
        PurifiedVanillaCard card,
        CardPlay cardPlay)
    {
        Player owner = card.Owner;
        switch (card)
        {
            case PurifiedBeckon when cardPlay.Target is not null:
                await CreatureCmd.Damage(
                    choiceContext, cardPlay.Target, 12m, DamageProps.cardHpLoss,
                    card, cardPlay);
                break;
            case PurifiedDebris:
                await PlayerCmd.GainEnergy(1m, owner);
                await CardPileCmd.Draw(choiceContext, 2, owner, false);
                await MoonlightMechanics.Gain(owner, 1m);
                break;
            case PurifiedFranticEscape when cardPlay.Target is not null:
                SandpitPower sandpit = owner.Creature.CombatState?.Enemies
                    .SelectMany(enemy => enemy.Powers.OfType<SandpitPower>())
                    .FirstOrDefault(power => power.Target == owner.Creature)
                    ?? cardPlay.Target.Powers.OfType<SandpitPower>().FirstOrDefault(power => power.Target == owner.Creature);
                if (sandpit is not null)
                {
                    await PowerCmd.ModifyAmount(
                        choiceContext, sandpit, 2m, sandpit.Owner, card);
                }
                break;
            case PurifiedSlimed:
                await CardPileCmd.Draw(choiceContext, 2, owner, false);
                await PowerCmd.Apply<SlipperyPower>(
                    choiceContext, owner.Creature, 1m, owner.Creature, card);
                break;
            case PurifiedToxic:
                await PowerCmd.Apply<ArtifactPower>(
                    choiceContext, owner.Creature, 1m, owner.Creature, card);
                break;
            case PurifiedSporeMind:
                await ResolveSporeMind(choiceContext, card);
                break;
            case PurifiedEnthralled:
                card.EnergyCost.AddThisCombat(1);
                await PlayerCmd.GainEnergy(3m, owner);
                await MoonlightMechanics.Gain(owner, 3m);
                await PowerCmd.Apply<EnergyNextTurnPower>(
                    choiceContext, owner.Creature, 3m, owner.Creature, card);
                await PowerCmd.Apply<EnthralledNextTurnMoonlightPower>(
                    choiceContext, owner.Creature, 3m, owner.Creature, card);
                break;
        }
    }

    public static async Task OnTurnEndInHand(
        PlayerChoiceContext choiceContext,
        PurifiedVanillaCard card)
    {
        switch (card)
        {
            case PurifiedBadLuck:
                await GenerateBadLuckReward(choiceContext, card);
                break;
            case PurifiedDazed:
                await ResolveDazed(choiceContext, card);
                break;
            case PurifiedDecay:
                await ResolveReportedEffect(choiceContext, card, false);
                break;
            case PurifiedRegret:
                await PowerCmd.Apply<VulnerablePower>(
                    choiceContext, card.Owner.Creature, 1m,
                    card.Owner.Creature, card);
                await PowerCmd.Apply<EnergyNextTurnPower>(
                    choiceContext, card.Owner.Creature, 2m,
                    card.Owner.Creature, card);
                break;
            case PurifiedDoubt:
                await PowerCmd.Apply<WeakPower>(
                    choiceContext, card.Owner.Creature, 1m, card.Owner.Creature, card);
                await PowerCmd.Apply<StrengthPower>(
                    choiceContext, card.Owner.Creature, 1m, card.Owner.Creature, card);
                break;
            case PurifiedBurn:
                await ResolveBurn(choiceContext, card);
                break;
            case PurifiedInfection or PurifiedToxic:
                await ResolvePoison(choiceContext, card);
                break;
            case PurifiedWither wither:
                await CreatureCmd.Heal(card.Owner.Creature, wither.HealingAmount);
                break;
        }
    }

    public static async Task AfterDrawn(
        PlayerChoiceContext choiceContext,
        PurifiedVanillaCard card)
    {
        switch (card)
        {
            case PurifiedVoid:
                await PlayerCmd.GainEnergy(2m, card.Owner);
                await MoonlightMechanics.Gain(card.Owner, 3m);
                break;
            case PurifiedCurseOfTheBell:
                await GrantVigorToAllies(
                    choiceContext,
                    card.Owner,
                    card.Owner.Relics.Count);
                break;
            case PurifiedInjury:
                await CreatureCmd.Heal(card.Owner.Creature, 4m);
                await MoonlightMechanics.Gain(card.Owner, 1m);
                break;
            case PurifiedSoot:
                await EnchantRandomAttack(card);
                break;
            case PurifiedWound:
                await PowerCmd.Apply<RegenPower>(
                    choiceContext, card.Owner.Creature, 2m,
                    card.Owner.Creature, card);
                break;
            case PurifiedPoorSleep poorSleep:
                AddPoorSleepPending(poorSleep);
                await SyncAnMianPower(choiceContext, card.Owner);
                break;
            case PurifiedNormality:
                break;
        }
    }

    public static async Task AfterExhausted(
        PlayerChoiceContext choiceContext,
        PurifiedVanillaCard card,
        bool fromHand)
    {
        if (!fromHand)
        {
            return;
        }

        switch (card)
        {
            case PurifiedShame:
                await PowerCmd.Apply<YueZhiZuZhouPower>(
                    choiceContext, card.Owner.Creature, 2m,
                    card.Owner.Creature, card);
                break;
            case PurifiedWrithe:
                await ResolveWritheEffect(choiceContext, card);
                break;
            case PurifiedBurn:
                await ResolveBurn(choiceContext, card);
                break;
            case PurifiedInfection:
                await ResolvePoison(choiceContext, card);
                break;
        }
    }

    public static async Task RecordDebtDiscard(
        PlayerChoiceContext choiceContext,
        PurifiedDebt debt)
    {
        if (debt.DiscardCount >= 5)
        {
            return;
        }

        debt.DiscardCount++;
        int triggerCount = 1 + YuanShaMechanics.GetExtraTriggerCount(debt);
        await AddDebtInterestReward(choiceContext, debt, 5 * triggerCount);
    }

    public static int RecordNormalityCardPlayed(PurifiedNormality normality) =>
        ++normality.CardsPlayedThisTurn;

    public static void ResetNormalityCounter(PurifiedNormality normality) =>
        normality.CardsPlayedThisTurn = 0;

    public static void AddPoorSleepPending(PurifiedPoorSleep card)
    {
        card.Pending++;
        card.DueFollowingTurn++;
    }

    public static int AdvancePoorSleep(PurifiedPoorSleep card)
    {
        int due = card.DueNextTurn;
        card.DueNextTurn = card.DueFollowingTurn;
        card.DueFollowingTurn = 0;
        card.Pending = Math.Max(0, card.Pending - due);
        return due;
    }

    public static async Task SyncAnMianPower(
        PlayerChoiceContext choiceContext,
        Player owner)
    {
        int pending = owner.PlayerCombatState.AllCards
            .OfType<PurifiedPoorSleep>()
            .Sum(card => card.Pending);
        AnMianPower power = owner.Creature.Powers.OfType<AnMianPower>().FirstOrDefault();
        if (pending <= 0)
        {
            if (power is not null)
            {
                await PowerCmd.Remove(power);
            }
            return;
        }

        if (power is null)
        {
            await PowerCmd.Apply<AnMianPower>(
                choiceContext, owner.Creature, pending, owner.Creature, null);
        }
        else if (power.Amount != pending)
        {
            await PowerCmd.ModifyAmount(
                choiceContext, power, pending - power.Amount,
                owner.Creature, null);
        }
    }

    public static async Task SyncTianZiPower(
        PlayerChoiceContext choiceContext,
        Player owner)
    {
        int cards = PileType.Hand.GetPile(owner).Cards
            .OfType<PurifiedNormality>()
            .Count();
        TianZiPower power = owner.Creature.Powers.OfType<TianZiPower>().FirstOrDefault();
        if (cards == 0)
        {
            if (power is not null)
            {
                await PowerCmd.Remove(power);
            }
            return;
        }

        if (power is null)
        {
            await PowerCmd.Apply<TianZiPower>(
                choiceContext, owner.Creature, cards, owner.Creature, null);
        }
        else if (power.Amount != cards)
        {
            await PowerCmd.ModifyAmount(
                choiceContext, power, cards - power.Amount, owner.Creature, null);
        }
    }

    private static async Task ApplySoar(
        PlayerChoiceContext choiceContext,
        CardModel source,
        decimal amount)
    {
        Player owner = source.Owner;
        SoarPower soar = owner.Creature.Powers.OfType<SoarPower>().FirstOrDefault();
        if (soar is null)
        {
            await PowerCmd.Apply<SoarPower>(
                choiceContext, owner.Creature, amount, owner.Creature, source);
        }
        else
        {
            await PowerCmd.ModifyAmount(
                choiceContext, soar, amount, owner.Creature, source);
        }

        if (owner.Creature.Powers.All(power => power is not DongmingSoarDurationPower))
        {
            await PowerCmd.Apply<DongmingSoarDurationPower>(
                choiceContext, owner.Creature, 1m, owner.Creature, source);
        }
    }

    private static async Task ResolveFolly(
        PlayerChoiceContext choiceContext,
        CardModel source)
    {
        IReadOnlyList<CardModel> drawPile = PileType.Draw.GetPile(source.Owner).Cards;
        if (drawPile.Count == 0)
        {
            return;
        }

        CardSelectorPrefs prefs = new(
            new LocString("cards",
                "SNOWFALLENMOD-DONGMING_VANILLA_MOON_PHASE.folly.selectionScreenPrompt"),
            0,
            Math.Min(5, drawPile.Count))
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
        IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext, drawPile, source.Owner, prefs);
        foreach (CardModel selectedCard in selected.ToList())
        {
            await CardCmd.Exhaust(choiceContext, selectedCard, false, false);
            await PowerCmd.Apply<DoomPower>(
                choiceContext,
                source.Owner.Creature,
                4m,
                source.Owner.Creature,
                source);
        }
    }

    private static async Task EnchantRandomAttack(CardModel source)
    {
        Player owner = source.Owner;
        List<CardModel> candidates = PileType.Hand.GetPile(owner).Cards
            .Where(card => card.Type == CardType.Attack && card.Enchantment is null)
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        CardModel selected = owner.RunState.Rng.CombatCardGeneration.NextItem(candidates);
        CardCmd.Enchant<TezcatarasEmber>(selected, 1m);
        await Task.CompletedTask;
    }

    private static int CountSporeMind(Player owner)
    {
        int copies = owner.PlayerCombatState.AllCards.Count(card =>
            card is PurifiedSporeMind &&
            card.Pile?.Type is PileType.Hand or PileType.Draw or PileType.Discard or PileType.Exhaust);
        return copies;
    }

    internal static decimal CalculateSporeMindPoison(Player owner) => 2m + 3m * CountSporeMind(owner);
    internal static decimal CalculateSporeMindCurse(Player owner) => 1m + CountSporeMind(owner);

    private static async Task ResolveSporeMind(
        PlayerChoiceContext choiceContext,
        CardModel source)
    {
        Player owner = source.Owner;
        decimal poison = CalculateSporeMindPoison(owner);
        decimal curse = CalculateSporeMindCurse(owner);
        foreach (Creature enemy in owner.Creature.CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<CursePower>(
                choiceContext, enemy, curse, owner.Creature, source);
            await PowerCmd.Apply<PoisonPower>(
                choiceContext, enemy, poison, owner.Creature, source);
        }

        CombatState combat = (CombatState)owner.Creature.CombatState;
        for (int i = 0; i < 1; i++)
        {
            PurifiedSporeMind generated = combat.CreateCard<PurifiedSporeMind>(owner);
            await CardPileCmd.AddGeneratedCardToCombat(
                generated, PileType.Discard, owner, CardPilePosition.Top);
        }
    }

    private static async Task ResolveDazed(
        PlayerChoiceContext choiceContext,
        CardModel source)
    {
        IReadOnlyList<Creature> enemies = source.Owner.Creature.CombatState.HittableEnemies;
        if (enemies.Count == 0)
        {
            return;
        }

        Creature target = source.Owner.RunState.Rng.CombatTargets.NextItem(enemies);
        await PowerCmd.Apply<DazedBrightStrengthDownPower>(
            choiceContext, target, 5m, source.Owner.Creature, source);
    }

    private static async Task ResolveBurn(
        PlayerChoiceContext choiceContext,
        CardModel source)
    {
        IReadOnlyList<Creature> enemies = source.Owner.Creature.CombatState.HittableEnemies;
        if (enemies.Count == 0)
        {
            return;
        }

        decimal highestStrength = enemies.Max(enemy =>
            enemy.Powers.OfType<StrengthPower>().FirstOrDefault()?.Amount ?? 0m);
        List<Creature> candidates = enemies.Where(enemy =>
            (enemy.Powers.OfType<StrengthPower>().FirstOrDefault()?.Amount ?? 0m) ==
            highestStrength).ToList();
        Creature target = source.Owner.RunState.Rng.CombatTargets.NextItem(candidates);
        await CreatureCmd.Damage(
            choiceContext, target, 8m, ValueProp.Move,
            source, null);
    }

    private static async Task ResolvePoison(
        PlayerChoiceContext choiceContext,
        CardModel source)
    {
        IReadOnlyList<Creature> enemies = source.Owner.Creature.CombatState.HittableEnemies;
        if (enemies.Count == 0)
        {
            return;
        }

        Creature target = source.Owner.RunState.Rng.CombatTargets.NextItem(enemies);
        await PowerCmd.Apply<PoisonPower>(
            choiceContext, target, 6m, source.Owner.Creature, source);
    }

    private static async Task ResolveReportedEffect(
        PlayerChoiceContext choiceContext,
        PurifiedVanillaCard card,
        bool cardAlreadyLeftHand)
    {
        if (card is PurifiedDecay)
        {
            await CreatureCmd.Heal(card.Owner.Creature, 4m);
            await MoonlightMechanics.Gain(card.Owner, 2m);
        }
        else if (card is PurifiedRegret)
        {
            decimal handSize = PileType.Hand.GetPile(card.Owner).Cards.Count +
                (cardAlreadyLeftHand ? 1m : 0m);
            // 悔恨：月华改为"手牌数的一半"（向下取整），治疗仍是手牌数×2。
            await MoonlightMechanics.Gain(card.Owner, Math.Floor(handSize / 2m));
            await CreatureCmd.Heal(card.Owner.Creature, handSize * 2m);
        }
    }

    private static async Task ResolveWritheEffect(
        PlayerChoiceContext choiceContext,
        PurifiedVanillaCard card)
    {
        await PowerCmd.Apply<DoomPower>(
            choiceContext,
            card.Owner.Creature,
            6m,
            card.Owner.Creature,
            card);
        foreach (Creature ally in card.Owner.Creature.CombatState.Players
                     .Select(player => player.Creature)
                     .Where(creature => creature.IsAlive))
        {
            await CreatureCmd.Heal(ally, 6m);
        }
    }

    private static async Task ApplyCurseAndDebuff<T>(
        PlayerChoiceContext choiceContext,
        CardModel source,
        decimal debuffAmount)
        where T : PowerModel
    {
        foreach (Creature enemy in source.Owner.Creature.CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<CursePower>(
                choiceContext, enemy, 1m, source.Owner.Creature, source);
            await PowerCmd.Apply<T>(
                choiceContext, enemy, debuffAmount,
                source.Owner.Creature, source);
        }
    }

    private static async Task GrantVigorToAllies(
        PlayerChoiceContext choiceContext,
        Player owner,
        decimal amount)
    {
        if (amount <= 0m)
        {
            return;
        }

        foreach (Creature ally in owner.Creature.CombatState.Players
                     .Select(player => player.Creature)
                     .Where(creature => creature.IsAlive))
        {
            await PowerCmd.Apply<VigorPower>(
                choiceContext,
                ally,
                amount,
                owner.Creature,
                null);
        }
    }

    private static async Task AddInterestReward(
        PlayerChoiceContext choiceContext,
        CardModel source,
        decimal amount)
    {
        Player owner = source.Owner;

        // 每张"利息"牌绑定自己的一条 LiXiPower，互不合并。
        // 这张表只存在于本次运行内，不写入存档、不保存引用。
        InterestPowerByCard.TryGetValue(source, out LiXiPower? power);
        if (power is null || !owner.Creature.Powers.Contains(power))
        {
            LiXiPower created = await PowerCmd.Apply<LiXiPower>(
                choiceContext, owner.Creature, amount, owner.Creature, source);
            if (created is not null)
            {
                InterestPowerByCard[source] = created;
            }
        }
        else
        {
            await PowerCmd.ModifyAmount(
                choiceContext, power, amount, owner.Creature, source);
        }
    }

    private static readonly Dictionary<CardModel, LiXiPower> InterestPowerByCard =
        new(ReferenceEqualityComparer.Instance);

    private static async Task AddDebtInterestReward(
        PlayerChoiceContext choiceContext,
        PurifiedDebt debt,
        int amount)
    {
        debt.InterestReward += amount;
        await AddInterestReward(choiceContext, debt, amount);
    }

    private static async Task RemoveInterestReward(
        PlayerChoiceContext choiceContext,
        CardModel source,
        decimal amount)
    {
        LiXiPower power = source.Owner.Creature.Powers
            .OfType<LiXiPower>()
            .FirstOrDefault();
        if (power is null)
        {
            return;
        }

        // 只处理"这张牌自己的"那条 buff（与 AddInterestReward 的绑定一致）
        if (InterestPowerByCard.TryGetValue(source, out LiXiPower? bound) &&
            !ReferenceEquals(bound, power))
        {
            power = bound;
        }

        if (power.Amount <= amount)
        {
            await PowerCmd.Remove(power);
        }
        else
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                power,
                -amount,
                source.Owner.Creature,
                source);
        }
    }

    private static async Task GenerateBadLuckReward(
        PlayerChoiceContext choiceContext,
        CardModel source)
    {
        Player owner = source.Owner;
        List<CardModel> candidates = ModelDb.AllCards.Where(card =>
            card.CanBeGeneratedByModifiers &&
            card.Type == CardType.Attack &&
            !card.EnergyCost.CostsX &&
            card.EnergyCost.Canonical == 3 &&
            card.MaxUpgradeLevel > 0).ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        CardModel canonical = owner.RunState.Rng.CombatCardGeneration.NextItem(candidates);
        CardModel generated = ((CombatState)owner.Creature.CombatState)
            .CreateCard(canonical, owner);
        if (generated.CurrentUpgradeLevel < generated.MaxUpgradeLevel)
        {
            CardCmd.Upgrade(generated);
        }
        generated.EnergyCost.SetThisCombat(0);
        await CardPileCmd.AddGeneratedCardToCombat(
            generated, PileType.Draw, owner, CardPilePosition.Top);
    }
}

