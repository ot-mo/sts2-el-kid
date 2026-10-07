using ElKid.ElKidCode.Powers;
using ElKid.ElKidCode.Time;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ElKid.ElKidCode.Cards;

/// <summary>FAST: Blink and draw. SLOW: smaller Blink and bank Time.</summary>
public class ShadowStep() : ElKidDualCard(1, CardType.Skill, CardRarity.Common, TargetType.Self, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [TimeResource.Tip, TimeResource.FastModeTip, ..ElKidCmd.BlinkTips];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar("Blink", 4m, ValueProp.Move), new BlockVar("SlowBlink", 3m, ValueProp.Move), new CardsVar(1), new DynamicVar("Time", 1m)];

    protected override async Task OnPlayFast(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ElKidCmd.Blink(choiceContext, Owner, DynamicVars["Blink"].BaseValue, this, cardPlay);
        await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].BaseValue, Owner);
    }

    protected override async Task OnPlaySlow(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ElKidCmd.Blink(choiceContext, Owner, DynamicVars["SlowBlink"].BaseValue, this, cardPlay);
        TimeResource.Gain(Owner, DynamicVars["Time"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Blink"].UpgradeValueBy(2m);
        DynamicVars["SlowBlink"].UpgradeValueBy(2m);
    }
}
