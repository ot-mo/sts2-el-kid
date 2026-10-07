using ElKid.ElKidCode.Cards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ElKid.ElKidCode.Time;

/// <summary>
/// Right-clicking a dual card in hand primes it to play its Fast half (see <see cref="ElKidDualCard.IsPrimed"/>).
/// The game only uses left-click on hand cards; right-click while dragging a card still cancels that play.
/// Prototype: priming is local state and is not synced in multiplayer yet.
/// </summary>
[HarmonyPatch(typeof(NHandCardHolder), "OnMousePressed")]
public static class PrimeOnRightClickPatch
{
    public static void Postfix(NHandCardHolder __instance, InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }) return;
        if (__instance.CardModel is not ElKidDualCard card) return;

        var hand = NPlayerHand.Instance;
        if (hand == null || hand.InCardPlay || hand.CurrentMode != NPlayerHand.Mode.Play) return;
        if (!FastModeButton.IsPlayPhase()) return;

        card.TogglePrimed();
    }
}
