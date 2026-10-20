using BaseLib.Utils;
using SnowfallenMod.SnowfallenCode.CardPools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

[Pool(typeof(SnowfallenHiddenCardPool))]
public class ShengZhang() : SnowfallenCard(
    1,
    CardType.Skill,
    CardRarity.Common,
    TargetType.Self)
{
    protected override bool IsPlayable =>
        base.IsPlayable &&
        (Owner is null || ShengZhangMechanics.UnitCount(Owner) < ShengZhangMechanics.MaxUnits);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ShengZhangMechanics.Summon(choiceContext, Owner, this);
    }
}
