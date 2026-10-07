using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using ElKid.ElKidCode.Character;
using ElKid.ElKidCode.Extensions;

namespace ElKid.ElKidCode.Potions;

[Pool(typeof(ElKidPotionPool))]
public abstract class ElKidPotion : CustomPotionModel
{
	public override string? CustomPackedImagePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();
	public override string? CustomPackedOutlinePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();
}