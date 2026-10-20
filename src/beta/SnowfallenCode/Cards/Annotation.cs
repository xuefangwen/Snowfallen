using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Enchantments;
using SnowfallenMod.SnowfallenCode.Powers;
namespace SnowfallenMod.SnowfallenCode.Cards;

public class Annotation : Ember
{
    public override CardType Type => CardType.Attack;
    public override CardRarity Rarity => CardRarity.Common;
    public override TargetType TargetType => TargetType.AllEnemies;
    public override bool CanBeGeneratedInCombat => true;
    public override HashSet<CardKeyword> CanonicalKeywords => [];
    public override bool IsAoe => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(1m, ValueProp.Move), new PowerVar<WaterBreathPower>(0m),
         new DynamicVar("Growth", 0m), new DynamicVar("MaterialReplay", 1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [AnnotationMark.InspectionTip];
    protected override void OnUpgrade() => DynamicVars["MaterialReplay"].UpgradeValueBy(1m);
}
