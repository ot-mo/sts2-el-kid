using BaseLib.Abstracts;
using ElKid.ElKidCode.Extensions;
using Godot;

namespace ElKid.ElKidCode.Character;

public class ElKidPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => ElKid.Color;
    

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}