using ElKid.ElKidCode.Time;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace ElKid.ElKidCode.Powers;

/// <summary>At the start of your turn, gain Time.</summary>
public class ClockworkHeartPower : ElKidPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [TimeResource.Tip];

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            Flash();
            TimeResource.Gain(player, Amount);
        }
        return Task.CompletedTask;
    }
}
