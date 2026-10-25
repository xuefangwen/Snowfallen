using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.DongmingCode.Enchantments;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.DongmingCode.Powers;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.DongmingCode.Cards;

public class YuJianPowerCard() : DongmingCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ZhouFanPower>(5m)];
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => PowerCmd.Apply<ZhouFanPower>(c, Owner.Creature, 5m, Owner.Creature, this);
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ZhouFanPower>()];
}

public class YongHengYueYan() : DongmingCard(1, CardType.Power, CardRarity.Ancient, TargetType.Self)
{
    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Innate];
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => PowerCmd.Apply<YongHengYueYanPower>(c, Owner.Creature, 1m, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public class ENian() : DongmingCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<CursePower>(3m)];
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) =>
        PowerCmd.Apply<ENianPower>(
            c,
            Owner.Creature,
            DynamicVars["CursePower"].BaseValue,
            Owner.Creature,
            this);
    protected override void OnUpgrade() =>
        DynamicVars["CursePower"].UpgradeValueBy(1m);
}

public class HuangZaiBian() : DongmingCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar("DoomPercent", 50m)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        HuangZaiBianPower existing = Owner.Creature.Powers
            .OfType<HuangZaiBianPower>()
            .FirstOrDefault();
        if (existing is not null)
        {
            decimal amount = Math.Floor(
                Owner.Creature.MaxHp * 50m / 100m);
            await PowerCmd.Apply<DoomPower>(c, Owner.Creature, amount, Owner.Creature, this);
            existing.QueueInstance(IsUpgraded);
            return;
        }

        if (IsUpgraded)
        {
            await PowerCmd.Apply<HuangZaiBianUpgradedPower>(
                c, Owner.Creature, 1m, Owner.Creature, this);
        }
        else
        {
            await PowerCmd.Apply<HuangZaiBianPower>(
                c, Owner.Creature, 1m, Owner.Creature, this);
        }

        // The first Doom stack is gained immediately when the card is played;
        // the power then supplies the second stack on the next turn and enters
        // Huang Disaster Form on the third turn.
        decimal initialDoom = Math.Floor(
            Owner.Creature.MaxHp * 50m / 100m);
        await PowerCmd.Apply<DoomPower>(
            c, Owner.Creature, initialDoom, Owner.Creature, this);
        Owner.Creature.Powers.OfType<HuangZaiBianPower>().FirstOrDefault()?.QueueInstance(IsUpgraded);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<EJieCounterPower>(),
         HoverTipFactory.FromPower<HuangZaiBianPower>()];
}

public class HuiShuang() : DongmingCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<HuiShuangPower>(3m)];
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => PowerCmd.Apply<HuiShuangPower>(c, Owner.Creature, DynamicVars["HuiShuangPower"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["HuiShuangPower"].UpgradeValueBy(2m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<FrostPower>()];
}

public class NingYue() : DongmingCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<NingYuePower>(2m)];
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => PowerCmd.Apply<NingYuePower>(c, Owner.Creature, DynamicVars["NingYuePower"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["NingYuePower"].UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<FrostPower>()];
}

public class CanXing() : DongmingCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<CanXingPower>(1m)];
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => PowerCmd.Apply<CanXingPower>(c, Owner.Creature, DynamicVars["CanXingPower"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["CanXingPower"].UpgradeValueBy(1m);
}

public class YuanSha() : DongmingCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => PowerCmd.Apply<YuanShaPower>(c, Owner.Creature, 1m, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public class YuHen() : DongmingCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<YuHenPower>(3m)];
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => PowerCmd.Apply<YuHenPower>(c, Owner.Creature, DynamicVars["YuHenPower"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["YuHenPower"].UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<CursePower>()];
}

public class HuoShaoYun() : DongmingCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => PowerCmd.Apply<HuoShaoYunPower>(c, Owner.Creature, IsUpgraded ? 1m : 1m, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<CursePower>(),
        HoverTipFactory.FromPower<OverCloudPower>()
    ];
}

public class ShiYue() : DongmingCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => PowerCmd.Apply<ShiYuePower>(c, Owner.Creature, 1m, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public class SuoSui() : DongmingCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override int CanonicalStarCost => 1;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<XuShiYinPower>(1m), new IntVar("DoomReduction", 8m)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        Wound wound = ((MegaCrit.Sts2.Core.Combat.CombatState)CombatState)
            .CreateCard<Wound>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(
            wound,
            PileType.Hand,
            Owner,
            CardPilePosition.Bottom);
        await DongmingAttackMechanics.ReduceDoom(
            c,
            Owner,
            DynamicVars["DoomReduction"].BaseValue,
            this);
        await PowerCmd.Apply<XuShiYinPower>(
            c,
            Owner.Creature,
            DynamicVars["XuShiYinPower"].BaseValue,
            Owner.Creature,
            this);
        await DongmingSkillMechanics.SelectAndToggleWithCurrentMarks(
            c,
            Owner,
            this,
            consumeMark: true);
    }
    protected override void OnUpgrade()
    {
        DynamicVars["DoomReduction"].UpgradeValueBy(4m);
        DynamicVars["XuShiYinPower"].UpgradeValueBy(1m);
    }
}

public class ShuangFeng() : DongmingCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override int CanonicalStarCost => 1;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<ShuangFengPower>(2m)];
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => PowerCmd.Apply<ShuangFengPower>(c, Owner.Creature, DynamicVars["ShuangFengPower"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["ShuangFengPower"].UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<NingHuaPower>()];
}

public class ShuangZhou() : DongmingCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<ShuangZhouArmorPower>(1m)];
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => PowerCmd.Apply<ShuangZhouArmorPower>(c, Owner.Creature, DynamicVars["ShuangZhouArmorPower"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["ShuangZhouArmorPower"].UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ShuangZhouArmorPower>()];
}

public class HuiQi() : DongmingCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(2), new CardsVar(2), new HealVar(5m)];
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) =>
        DynamicVars.Energy.BaseValue >= 3m
        ? PowerCmd.Apply<HuiQiUpgradedPower>(c, Owner.Creature, 1m, Owner.Creature, this)
        : PowerCmd.Apply<HuiQiPower>(c, Owner.Creature, 1m, Owner.Creature, this);
    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
        DynamicVars.Cards.UpgradeValueBy(1m);
        DynamicVars.Heal.UpgradeValueBy(3m);
    }
}

public class ZhouFu() : DongmingCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override int CanonicalStarCost => 1;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<XuShiYinPower>(1m)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        List<CardModel> candidates = ZhiyuanStatusPool.GetCandidates();
        MegaCrit.Sts2.Core.Combat.CombatState combat =
            (MegaCrit.Sts2.Core.Combat.CombatState)CombatState;
        List<CardModel> options = [];
        for (int index = 0; index < Math.Min(3, candidates.Count); index++)
        {
            int choice = Owner.RunState.Rng.CombatCardGeneration
                .NextInt(candidates.Count);
            options.Add(combat.CreateCard(candidates[choice], Owner));
            candidates.RemoveAt(choice);
        }
        if (options.Count > 0)
        {
            CardModel selected = (await CardSelectCmd.FromSimpleGrid(
                c,
                options,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1, 1)
                {
                    Cancelable = false,
                    RequireManualConfirmation = true
                })).First();
            await CardPileCmd.AddGeneratedCardToCombat(
                selected,
                PileType.Hand,
                Owner,
                CardPilePosition.Bottom);
        }
        await PowerCmd.Apply<XuShiYinPower>(
            c,
            Owner.Creature,
            DynamicVars["XuShiYinPower"].BaseValue,
            Owner.Creature,
            this);
        await DongmingSkillMechanics.SelectAndToggleWithCurrentMarks(
            c,
            Owner,
            this,
            consumeMark: true);
    }
    protected override void OnUpgrade() =>
        DynamicVars["XuShiYinPower"].UpgradeValueBy(1m);
}

public class XuRui() : DongmingCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<XuRuiPower>(4m)];
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => PowerCmd.Apply<XuRuiPower>(c, Owner.Creature, DynamicVars["XuRuiPower"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["XuRuiPower"].UpgradeValueBy(2m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<FrostPower>()];
}

