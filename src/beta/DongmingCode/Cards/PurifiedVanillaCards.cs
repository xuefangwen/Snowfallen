using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.DongmingCode.Cards;

public abstract class PurifiedVanillaCard(
    int cost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    string portraitFile,
    bool hasTurnEndEffect,
    params CardKeyword[] keywords) :
    DongmingCard(cost, type, rarity, target)
{
    private readonly CardKeyword[] _keywords = keywords;

    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override bool ShouldReceiveCombatHooks => true;
    public override bool HasTurnEndInHandEffect => hasTurnEndEffect;
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;
    protected override bool ShouldGlowRedInternal => false;
    public override string PortraitPath => portraitFile.CardImagePath();
    public override string CustomPortraitPath => portraitFile.CardImagePath();
    public override string BetaPortraitPath => portraitFile.CardImagePath();
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [PurificationMechanics.GetOriginalHoverTip(this)];

    internal async Task ResolvePurified(
        PlayerChoiceContext choiceContext)
    {
        int triggerCount = 1 +
            YuanShaMechanics.GetExtraTriggerCount(this);
        for (int index = 0; index < triggerCount; index++)
        {
            await PurifiedVanillaEffects.OnPurified(
                choiceContext,
                this);
        }
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) =>
        PurifiedVanillaEffects.OnPlay(choiceContext, this, cardPlay);

    protected override Task OnTurnEndInHand(
        PlayerChoiceContext choiceContext) =>
        PurifiedVanillaEffects.OnTurnEndInHand(choiceContext, this);

    public override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw)
    {
        if (!ReferenceEquals(card, this))
        {
            return;
        }

        int triggerCount = 1 + YuanShaMechanics.GetExtraTriggerCount(this);
        for (int index = 0; index < triggerCount; index++)
        {
            await PurifiedVanillaEffects.AfterDrawn(choiceContext, this);
        }
    }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (this is PurifiedEnthralled &&
            autoPlayType == AutoPlayType.None &&
            Pile?.Type == PileType.Hand &&
            card.Owner == Owner &&
            !ReferenceEquals(card, this))
        {
            return false;
        }

        return true;
    }
}

public sealed class PurifiedAscendersBane() : PurifiedVanillaCard(
    -1, CardType.Curse, CardRarity.Curse, TargetType.None,
    "ascenders_bane_bright.png", false,
    CardKeyword.Eternal, CardKeyword.Unplayable, CardKeyword.Ethereal);

public sealed class PurifiedBadLuck() : PurifiedVanillaCard(
    -1, CardType.Curse, CardRarity.Curse, TargetType.None,
    "bad_luck_bright.png", true, CardKeyword.Eternal, CardKeyword.Unplayable);

public sealed class PurifiedBeckon() : PurifiedVanillaCard(
    1, CardType.Status, CardRarity.Status, TargetType.AnyEnemy,
    "beckon_bright.png", false);

public sealed class PurifiedBurn() : PurifiedVanillaCard(
    -1, CardType.Status, CardRarity.Status, TargetType.None,
    "burn_bright.png", true, CardKeyword.Unplayable);

public sealed class PurifiedClumsy() : PurifiedVanillaCard(
    -1, CardType.Curse, CardRarity.Curse, TargetType.None,
    "clumsy_bright.png", false, CardKeyword.Unplayable, CardKeyword.Ethereal);

public sealed class PurifiedCurseOfTheBell() : PurifiedVanillaCard(
    -1, CardType.Curse, CardRarity.Curse, TargetType.None,
    "curse_of_the_bell_bright.png", true,
    CardKeyword.Eternal, CardKeyword.Unplayable);

public sealed class PurifiedDazed() : PurifiedVanillaCard(
    -1, CardType.Status, CardRarity.Status, TargetType.None,
    "dazed_bright.png", true, CardKeyword.Ethereal, CardKeyword.Unplayable);

public sealed class PurifiedDebris() : PurifiedVanillaCard(
    0, CardType.Status, CardRarity.Status, TargetType.None,
    "debris_bright.png", false, CardKeyword.Exhaust);

public sealed class PurifiedDebt() : PurifiedVanillaCard(
    -1, CardType.Curse, CardRarity.Curse, TargetType.None,
    "debt_bright.png", false, CardKeyword.Unplayable)
{
    [SavedProperty]
    public int DiscardCount { get; set; }

    [SavedProperty]
    public int InterestReward { get; set; }
}

public sealed class PurifiedDecay() : PurifiedVanillaCard(
    -1, CardType.Curse, CardRarity.Curse, TargetType.None,
    "decay_bright.png", true, CardKeyword.Unplayable);

public sealed class PurifiedDoubt() : PurifiedVanillaCard(
    -1, CardType.Curse, CardRarity.Curse, TargetType.None,
    "doubt_bright.png", true, CardKeyword.Unplayable);

public sealed class PurifiedEnthralled() : PurifiedVanillaCard(
    0, CardType.Curse, CardRarity.Curse, TargetType.None,
    "enthralled_bright.png", false, CardKeyword.Eternal);

public sealed class PurifiedFolly() : PurifiedVanillaCard(
    -1, CardType.Curse, CardRarity.Curse, TargetType.None,
    "folly_bright.png", false,
    CardKeyword.Unplayable, CardKeyword.Eternal, CardKeyword.Innate,
    CardKeyword.Ethereal, CardKeyword.Exhaust);

public sealed class PurifiedFranticEscape() : PurifiedVanillaCard(
    1, CardType.Status, CardRarity.Status, TargetType.AnyEnemy,
    "frantic_escape_bright.png", false);

public sealed class PurifiedGreed() : PurifiedVanillaCard(
    -1, CardType.Curse, CardRarity.Curse, TargetType.None,
    "greed_bright.png", false,
    CardKeyword.Eternal, CardKeyword.Unplayable, CardKeyword.Ethereal);

public sealed class PurifiedGuilty() : PurifiedVanillaCard(
    -1, CardType.Curse, CardRarity.Curse, TargetType.None,
    "guilty_bright.png", false, CardKeyword.Unplayable);

public sealed class PurifiedInfection() : PurifiedVanillaCard(
    -1, CardType.Status, CardRarity.Status, TargetType.None,
    "infection_bright.png", true, CardKeyword.Unplayable);

public sealed class PurifiedInjury() : PurifiedVanillaCard(
    -1, CardType.Curse, CardRarity.Curse, TargetType.None,
    "injury_bright.png", false, CardKeyword.Unplayable);

public sealed class PurifiedNormality() : PurifiedVanillaCard(
    -1, CardType.Curse, CardRarity.Curse, TargetType.None,
    "normality_bright.png", false, CardKeyword.Unplayable)
{
    [SavedProperty]
    public int CardsPlayedThisTurn { get; set; }
}

public sealed class PurifiedPoorSleep() : PurifiedVanillaCard(
    -1, CardType.Curse, CardRarity.Curse, TargetType.None,
    "poor_sleep_bright.png", false, CardKeyword.Unplayable, CardKeyword.Retain)
{
    [SavedProperty] public int Pending { get; set; }
    [SavedProperty] public int DueNextTurn { get; set; }
    [SavedProperty] public int DueFollowingTurn { get; set; }
}

public sealed class PurifiedRegret() : PurifiedVanillaCard(
    -1, CardType.Curse, CardRarity.Curse, TargetType.None,
    "regret_bright.png", true, CardKeyword.Unplayable);

public sealed class PurifiedShame() : PurifiedVanillaCard(
    -1, CardType.Curse, CardRarity.Curse, TargetType.None,
    "shame_bright.png", true, CardKeyword.Unplayable, CardKeyword.Ethereal);

public sealed class PurifiedSlimed() : PurifiedVanillaCard(
    1, CardType.Status, CardRarity.Status, TargetType.None,
    "slimed_bright.png", false, CardKeyword.Exhaust);

public sealed class PurifiedSoot() : PurifiedVanillaCard(
    -1, CardType.Status, CardRarity.Status, TargetType.None,
    "soot_bright.png", false, CardKeyword.Unplayable, CardKeyword.Ethereal);

public sealed class PurifiedSporeMind() : PurifiedVanillaCard(
    1, CardType.Curse, CardRarity.Curse, TargetType.None,
    "spore_mind_bright.png", false, CardKeyword.Exhaust);

public sealed class PurifiedToxic() : PurifiedVanillaCard(
    1, CardType.Status, CardRarity.Status, TargetType.None,
    "toxic_bright.png", true, CardKeyword.Exhaust);

public sealed class PurifiedVoid() : PurifiedVanillaCard(
    -1, CardType.Status, CardRarity.Status, TargetType.None,
    "void_bright.png", false, CardKeyword.Unplayable, CardKeyword.Ethereal);

public sealed class PurifiedWither() : PurifiedVanillaCard(
    -1, CardType.Status, CardRarity.Status, TargetType.None,
    "wither_bright.png", true, CardKeyword.Unplayable)
{
    [SavedProperty]
    public int HealingAmount
    {
        get => RottenWoodEthereal ? 5 : (int)DynamicVars.Damage.BaseValue;
        set => DynamicVars.Damage.BaseValue = RottenWoodEthereal ? 5 : value;
    }

    private bool _rottenWoodEthereal;
    [SavedProperty]
    public bool RottenWoodEthereal
    {
        get => _rottenWoodEthereal;
        set
        {
            bool wasRottenWood = _rottenWoodEthereal;
            _rottenWoodEthereal = value;
            if (!IsMutable) return;
            if (value) { AddKeyword(CardKeyword.Ethereal); DynamicVars.Damage.BaseValue = 5; }
            else if (wasRottenWood) RemoveKeyword(CardKeyword.Ethereal);
        }
    }
    public override IEnumerable<CardKeyword> CanonicalKeywords => RottenWoodEthereal
        ? [CardKeyword.Unplayable, CardKeyword.Ethereal] : base.CanonicalKeywords;
    protected override void AddExtraArgsToDescription(MegaCrit.Sts2.Core.Localization.LocString description)
    {
        base.AddExtraArgsToDescription(description);
        var text = new MegaCrit.Sts2.Core.Localization.LocString("cards", Id.Entry +
            (RottenWoodEthereal ? ".rottenWoodDescription" : ".normalDescription"));
        DynamicVars.AddTo(text);
        description.Add("WitherDescription", text.GetFormattedText());
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(3m, ValueProp.Unpowered | ValueProp.Move)];
}

public sealed class PurifiedWound() : PurifiedVanillaCard(
    -1, CardType.Status, CardRarity.Status, TargetType.None,
    "wound_bright.png", false, CardKeyword.Unplayable);

public sealed class PurifiedWrithe() : PurifiedVanillaCard(
    -1, CardType.Curse, CardRarity.Curse, TargetType.None,
    "writhe_bright.png", false, CardKeyword.Innate, CardKeyword.Unplayable)
{
    [SavedProperty]
    public int PendingNextTurnTriggers { get; set; }
}

