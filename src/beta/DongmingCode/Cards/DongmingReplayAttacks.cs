using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.DongmingCode.Powers;

namespace SnowfallenMod.DongmingCode.Cards;

public class ZhanE() : DongmingCard(
    1,
    CardType.Attack,
    CardRarity.Rare,
    TargetType.RandomEnemy)
{
    public override int CanonicalStarCost => 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DoomPower>(),
        HoverTipFactory.FromPower<ExhaustionPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new IntVar("DoomReduction", 3m),
        new RepeatVar(4)
    ];

    public int CurseReplayCount => Math.Min(
        DynamicVars.Repeat.IntValue,
        PileType.Exhaust.GetPile(Owner).Cards.Count(
            MoonlightMechanics.IsCursedCard));

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await DongmingAttackMechanics.ReduceDoom(
            choiceContext,
            Owner,
            DynamicVars["DoomReduction"].BaseValue,
            this);
        if (CombatState.HittableEnemies.Count == 0)
        {
            return;
        }

        Creature target = DongmingAttackMechanics.RandomEnemy(Owner);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(2m);
    }
}

public class QingHui() : DongmingCard(
    1,
    CardType.Attack,
    CardRarity.Rare,
    TargetType.AnyEnemy)
{
    public override int CanonicalStarCost => 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move),
        new RepeatVar(2)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);

        List<CardModel> candidates = PileType.Discard.GetPile(Owner).Cards
            .Where(card => card.Type is CardType.Attack or CardType.Skill or CardType.Power)
            .Where(card => card.BaseReplayCount + card.GetEnchantedReplayCount() == 0)
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        CardModel selected = Owner.RunState.Rng.CombatCardGeneration
            .NextItem(candidates);
        selected.BaseReplayCount = DynamicVars.Repeat.IntValue;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}

public class CuoHui() : DongmingCard(
    0,
    CardType.Attack,
    CardRarity.Rare,
    TargetType.RandomEnemy)
{
    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new IntVar("EnergyDamage", 4m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        int hitCount = ResolveEnergyXValue();
        decimal damage = DynamicVars.Damage.BaseValue +
                         hitCount * DynamicVars["EnergyDamage"].BaseValue;

        if (hitCount > 0 && CombatState.HittableEnemies.Count > 0)
        {
            await DamageCmd.Attack(damage)
                .FromCard(this, cardPlay)
                .TargetingRandomOpponents(CombatState)
                .WithHitCount(hitCount)
                .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["EnergyDamage"].UpgradeValueBy(1m);
    }
}


