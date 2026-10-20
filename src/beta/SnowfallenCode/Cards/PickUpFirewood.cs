using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class PickUpFirewood() : SnowfallenCard(
    0,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int emberCount = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0);
        if (emberCount <= 0)
        {
            return;
        }

        await EmberMechanics.CreateInHand(
            Owner,
            CombatState,
            emberCount);
        await CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars.Block.BaseValue * emberCount,
            ValueProp.Move,
            cardPlay);
    }

    protected override void OnUpgrade()
    {
    }
}

