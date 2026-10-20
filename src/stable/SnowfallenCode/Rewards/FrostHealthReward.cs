using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace SnowfallenMod.SnowfallenCode.Rewards;

public sealed class FrostHealthReward : GoldReward
{
    private const string FrostIconPath =
        "res://Snowfallen/Images/Powers/frost_power.png";
    private const string DescriptionKey = "SNOWFALLENMOD-FROST_HEALTH_REWARD.description";

    public FrostHealthReward(int amount, Player player)
        : base(amount, player, wasGoldStolenBack: false)
    {
    }

    protected override string IconPath => FrostIconPath;

    public override LocString Description
    {
        get
        {
            LocString description = new("powers", DescriptionKey);
            description.Add("Amount", Amount);
            return description;
        }
    }

    protected override async Task<bool> OnSelect()
    {
        await CreatureCmd.Heal(Player.Creature, Amount);
        return true;
    }

    public override SerializableReward ToSerializable()
    {
        // RewardType.None plus a negative GoldAmount is reserved by Snowfallen
        // for restoring this custom reward when saving on the reward screen.
        return new SerializableReward
        {
            RewardType = RewardType.None,
            GoldAmount = -Amount
        };
    }
}
