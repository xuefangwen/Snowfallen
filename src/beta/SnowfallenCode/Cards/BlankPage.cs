using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class BlankPage() : SnowfallenCard(
    1,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ArtifactPower>(),
        HoverTipFactory.FromPower<DrawCardsNextTurnPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ArtifactPower>(1m),
        new CardsVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CombatState state = (CombatState)CombatState;
        bool dealtDamageBeforePlay = CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Any(entry =>
                entry.HappenedThisTurn(state) &&
                entry.Dealer == Owner.Creature &&
                entry.Receiver.Side != Owner.Creature.Side &&
                entry.Result.TotalDamage > 0);

        bool isFirstPlayThisCombat = Owner.Creature.Powers
            .OfType<PreviewUsedPower>()
            .FirstOrDefault() is null;
        if (isFirstPlayThisCombat)
        {
            await PowerCmd.Apply<ArtifactPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["ArtifactPower"].BaseValue,
                Owner.Creature,
                this);
            await PowerCmd.Apply<PreviewUsedPower>(
                choiceContext,
                Owner.Creature,
                1m,
                Owner.Creature,
                this);
        }
        if (!dealtDamageBeforePlay)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
            await PowerCmd.Apply<DrawCardsNextTurnPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars.Cards.BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

