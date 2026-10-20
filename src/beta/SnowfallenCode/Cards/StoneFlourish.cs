using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
namespace SnowfallenMod.SnowfallenCode.Cards;

public class StoneFlourish() : SnowfallenCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(14m, ValueProp.Move)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<RottenWood>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3").Execute(context);
        if (CombatManager.Instance.IsOverOrEnding || Owner.Creature.IsDead) return;
        var candidates = PileType.Hand.GetPile(Owner).Cards.Where(card => !ReferenceEquals(card, this)).ToList();
        if (candidates.Count == 0) return;
        var selected = (await CardSelectCmd.FromHand(context, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1, 1), candidates.Contains, this)).SingleOrDefault();
        if (selected is null || selected.Owner != Owner || selected.Pile != PileType.Hand.GetPile(Owner)) return;
        await CardPileCmd.RemoveFromCombat(selected);
        if (CombatManager.Instance.IsOverOrEnding || Owner.Creature.IsDead) return;
        var wood = ((CombatState)Owner.Creature.CombatState).CreateCard<RottenWood>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(wood, PileType.Hand, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(7m);
}
