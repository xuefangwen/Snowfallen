using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;
using SnowfallenMod.DongmingCode.Mechanics;

namespace SnowfallenMod.DongmingCode.Relics;

public class HuLaoBanCheck : DongmingRelics
{
    public const int GoldPerMoonlight = 150;

    private int _spentGold;

    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool ShowCounter => true;
    public override int DisplayAmount => SpentGold;

    [SavedProperty]
    public int SpentGold
    {
        get => _spentGold;
        private set
        {
            AssertMutable();
            _spentGold = Math.Max(0, value);
        }
    }

    public void RecordSpentGold(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int previousReward = SpentGold / GoldPerMoonlight;
        SpentGold = amount > int.MaxValue - SpentGold
            ? int.MaxValue
            : SpentGold + amount;
        InvokeDisplayAmountChanged();

        if (SpentGold / GoldPerMoonlight > previousReward)
        {
            Flash();
        }
    }

    public override async Task BeforeCombatStart()
    {
        int moonlight = SpentGold / GoldPerMoonlight;
        if (moonlight <= 0)
        {
            return;
        }

        Flash();
        await MoonlightMechanics.Gain(Owner, moonlight);
    }
}
