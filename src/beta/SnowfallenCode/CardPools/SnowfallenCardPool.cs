using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.SnowfallenCode.Character;

public class SnowfallenCardPool : CustomCardPoolModel
{
    public override string Title => Snowfallen.CharacterId; //This is not a display name.

    public override string BigEnergyIconPath => "Charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "Charui/text_energy.png".ImagePath();

    // Custom frames are authored at their final colors, so the shader must be neutral.
    public override float H => 1f;
    public override float S => 1f;
    public override float V => 1f;

    public override Texture2D CustomFrame(CustomCardModel card)
    {
        string frameName = card.Type switch
        {
            CardType.Attack => "snowfallen_card_frame_attack.png",
            CardType.Skill => "snowfallen_card_frame_skill.png",
            CardType.Power => "snowfallen_card_frame_power.png",
            _ => "snowfallen_card_frame_skill.png"
        };

        return PreloadManager.Cache.GetTexture2D(
            Path.Join("Cards", "Frames", frameName).ImagePath());
    }

    //Color of small card icons
    public override Color DeckEntryCardColor => new("840240");
    public override Color EnergyOutlineColor => new("135F91");

    public override bool IsColorless => false;
}

