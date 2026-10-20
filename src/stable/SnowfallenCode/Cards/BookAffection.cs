using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class BookAffection() : SnowfallenCard(
    2,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(4),
        new HealVar(3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int embers = PileType.Hand.GetPile(Owner).Cards.OfType<IEmberCard>().Count();
        await CardPileCmd.Draw(
            choiceContext,
            DynamicVars.Cards.BaseValue + embers,
            Owner);
        if (embers > 0)
        {
            await CreatureCmd.Heal(
                Owner.Creature,
                DynamicVars.Heal.BaseValue * embers);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
        DynamicVars.Heal.UpgradeValueBy(1m);
    }
}
