using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

// Legacy compatibility model. Old saves may still contain the original
// PROLOGUE id, so keep it loadable but remove it from the reward pool.
public class Prologue() : SnowfallenCard(
    3,
    CardType.Power,
    CardRarity.Token,
    TargetType.Self)
{
    public override bool CanBeGeneratedInCombat => false;

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await PowerCmd.Apply<MysticFoxTransformationPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
    }
}
