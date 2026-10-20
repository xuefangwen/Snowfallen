using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace SnowfallenMod.SnowfallenCode.Powers;

public abstract class RemainingWarmthPowerBase : SnowfallenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task<bool> TryClaimNextEmber(PlayerChoiceContext choiceContext)
    {
        if (Amount <= 0m)
        {
            return false;
        }

        Flash();
        if (Amount <= 1m)
        {
            await PowerCmd.Remove(this);
        }
        else
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                this,
                -1m,
                Owner,
                null);
        }

        return true;
    }
}

public class RemainingWarmthPower : RemainingWarmthPowerBase;

public class UpgradedRemainingWarmthPower : RemainingWarmthPowerBase;
