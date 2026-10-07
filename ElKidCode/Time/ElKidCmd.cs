using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ElKid.ElKidCode.Time;

/// <summary>Shared El Kid actions used by cards and powers.</summary>
public static class ElKidCmd
{
    /// <summary>Blink X: gain X Block and X Vigor (your next Attack deals X additional damage).</summary>
    public static async Task Blink(PlayerChoiceContext choiceContext, Player player, decimal amount, CardModel? card, CardPlay? cardPlay)
    {
        await CreatureCmd.GainBlock(player.Creature, amount, ValueProp.Move, cardPlay);
        await PowerCmd.Apply<VigorPower>(choiceContext, player.Creature, amount, player.Creature, card);
    }

    /// <summary>Lose HP from your own card (unblockable), like the base game's Offering.</summary>
    public static async Task LoseHp(PlayerChoiceContext choiceContext, Player player, decimal amount, CardModel card, CardPlay? cardPlay)
    {
        await CreatureCmd.Damage(choiceContext, player.Creature, amount,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, card, cardPlay);
    }

    public static IHoverTip BlinkTip => new HoverTip(
        new LocString("static_hover_tips", "ELKID-BLINK.title"),
        new LocString("static_hover_tips", "ELKID-BLINK.description"),
        null);

    public static IEnumerable<IHoverTip> BlinkTips => [BlinkTip, HoverTipFactory.FromPower<VigorPower>()];
}
