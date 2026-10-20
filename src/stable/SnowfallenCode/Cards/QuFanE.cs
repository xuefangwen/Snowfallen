using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class QuFanE() : SnowfallenCard(
    2,
    CardType.Attack,
    CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(16m, ValueProp.Move),
        new CardsVar(2),
        new ExtraDamageVar(5m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        List<CardModel> candidates = PileType.Hand.GetPile(Owner).Cards
            .Where(card =>
                !ReferenceEquals(card, this) &&
                card.Type is CardType.Curse or CardType.Status)
            .ToList();
        int maximum = Math.Min(DynamicVars.Cards.IntValue, candidates.Count);
        List<CardModel> selected = maximum == 0
            ? []
            : (await CardSelectCmd.FromHand(
                    choiceContext,
                    Owner,
                    new CardSelectorPrefs(
                        SelectionScreenPrompt,
                        0,
                        maximum),
                    card => candidates.Contains(card),
                    this))
                .ToList();

        foreach (CardModel card in selected)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        decimal damage = DynamicVars.Damage.BaseValue +
                         selected.Count * DynamicVars.ExtraDamage.BaseValue;
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars.Cards.UpgradeValueBy(1m);
        DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }
}
