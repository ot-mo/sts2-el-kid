using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ElKid.ElKidCode.Time;

/// <summary>
/// Implemented by powers that react to "whenever you play a card Fast".
/// Called by <see cref="Cards.ElKidDualCard"/> after its Fast half resolves.
/// </summary>
public interface IOnFastPlay
{
    Task AfterFastPlay(PlayerChoiceContext choiceContext, CardModel card);
}
