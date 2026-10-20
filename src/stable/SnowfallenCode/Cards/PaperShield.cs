using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class PaperShield() : SnowfallenCard(
    0,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    public override HashSet<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(3m, ValueProp.Unpowered)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先从消耗牌堆取回 1 张余烬（可不选）。
        List<IEmberCard> candidates = PileType.Exhaust.GetPile(Owner).Cards
            .OfType<IEmberCard>()
            .ToList();
        if (candidates.Count > 0)
        {
            IEmberCard selected = (await CardSelectCmd.FromSimpleGrid(
                    choiceContext,
                    candidates.Select(candidate => candidate.Card).ToList(),
                    Owner,
                    new CardSelectorPrefs(SelectionScreenPrompt, 0, 1)))
                .OfType<IEmberCard>()
                .FirstOrDefault();
            if (selected is not null)
            {
                EmberTrackerPower tracker = await EmberMechanics.EnsureTracker(
                    choiceContext,
                    Owner,
                    CombatState,
                    this);
                tracker.RegisterNewActiveEmber(selected);
                await CardPileCmd.Add(selected.Card, PileType.Hand);
            }
        }

        await PowerCmd.Apply<PaperShieldPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars.Block.BaseValue,
            Owner.Creature,
            this);
        Owner.Creature.Powers
            .OfType<PaperShieldPower>()
            .FirstOrDefault()
            ?.RefreshDuration();
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1m);
    }
}
