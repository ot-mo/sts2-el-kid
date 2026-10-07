using ElKid.ElKidCode.Time;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ElKid.ElKidCode.Powers;

/// <summary>Whenever you play a card Fast, gain Block.</summary>
public class QuicksilverPower : ElKidPower, IOnFastPlay
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Block), TimeResource.FastModeTip];

    public async Task AfterFastPlay(PlayerChoiceContext choiceContext, CardModel card)
    {
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null, fast: true);
    }
}
