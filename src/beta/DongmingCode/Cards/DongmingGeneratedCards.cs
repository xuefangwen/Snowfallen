using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using SnowfallenMod.DongmingCode.Powers;
using SnowfallenMod.DongmingCode.Mechanics;

namespace SnowfallenMod.DongmingCode.Cards;

public class YueHua() : DongmingCard(
    0,
    CardType.Skill,
    CardRarity.Token,
    TargetType.Self)
{
    public override bool CanBeGeneratedInCombat => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new StarsVar(2)];

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) =>
        ResolvePlay(choiceContext);

    private async Task ResolvePlay(PlayerChoiceContext choiceContext)
    {
        await MoonlightMechanics.Gain(Owner, DynamicVars.Stars.BaseValue);
        await PowerCmd.Apply<NextMoonlightFreePower>(
            choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Stars.UpgradeValueBy(1m);
    }
}

public class CanShi() : MoonPhaseCard(
    1,
    CardType.Curse,
    CardRarity.Token,
    TargetType.Self,
    MoonPhase.Dark)
{
    protected override bool ShowOtherPhaseInHoverTipOnly => true;
    public override bool CanBeGeneratedInCombat => false;
    public override bool HasTurnEndInHandEffect => true;
    public override int MaxUpgradeLevel => 0;
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    public override string Title => CurrentMoonPhase == MoonPhase.Bright
        ? new LocString("cards", $"{Id.Entry}.title.bright").GetFormattedText()
        : base.Title;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar("Doom", 2m)];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (CurrentMoonPhase == MoonPhase.Bright)
        {
            await MoonlightMechanics.Gain(Owner, 2m);
        }
    }

    protected override async Task OnTurnEndInHand(
        PlayerChoiceContext choiceContext)
    {
        if (CurrentMoonPhase != MoonPhase.Dark)
        {
            return;
        }

        await PowerCmd.Apply<DoomPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["Doom"].BaseValue,
            Owner.Creature,
            this);
    }
}

