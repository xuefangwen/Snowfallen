using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.DongmingCode.Cards;
using SnowfallenMod.DongmingCode.Character;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.DongmingCode.CardPools;

public class DongmingCardPool : CustomCardPoolModel
{
    public override string Title => Dongming.CharacterId;

    public override IEnumerable<CardModel> AllCards =>
        base.AllCards
            .Where(card => card is not CanGuang and not ZhaoYing and not CanXing)
            .Where(card => card is not ZhuiYi and not LunHui and not EZhao
                and not XuZai and not XiangCan and not FuHua
                and not TongMing and not MingJie);

    public override string BigEnergyIconPath => "Charui/DongmingEnergy/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "Charui/DongmingEnergy/text_energy.png".ImagePath();

    // The PNGs already contain their final colors; keep the runtime tint
    // neutral so the blue-white-blue gradient is not recolored again.
    public override float H => 1f;
    public override float S => 1f;
    public override float V => 1f;

    // Static pre-colored frames preserve the vanilla frame geometry and alpha
    // mask while providing the blue-white-blue Dongming palette.
    public override Texture2D CustomFrame(CustomCardModel card)
    {
        string frameName = card.Type switch
        {
            CardType.Attack => "dongming_card_frame_attack.png",
            CardType.Skill => "dongming_card_frame_skill.png",
            CardType.Power => "dongming_card_frame_power.png",
            _ => "dongming_card_frame_skill.png"
        };

        return PreloadManager.Cache.GetTexture2D(
            Path.Join("Cards", "Frames", frameName).ImagePath());
    }

    public override Color DeckEntryCardColor => new("CD4EED");
    public override Color EnergyOutlineColor => new("803367");
    public override bool IsColorless => false;

}

