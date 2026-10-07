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

/// <summary>FAST: Block and Weaken an enemy. SLOW: more Block and bank Time.</summary>
public class Parry() : ElKidDualCard(1, CardType.Skill, CardRarity.Common, TargetType.Self, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [TimeResource.Tip, TimeResource.FastModeTip, HoverTipFactory.FromPower<WeakPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar("Block", 5m, ValueProp.Move), new BlockVar("SlowBlock", 7m, ValueProp.Move), new PowerVar<WeakPower>(2m), new DynamicVar("Time", 1m)];

    protected override async Task OnPlayFast(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["Block"].BaseValue, ValueProp.Move, cardPlay);
        await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target!, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
    }

    protected override async Task OnPlaySlow(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["SlowBlock"].BaseValue, ValueProp.Move, cardPlay);
        TimeResource.Gain(Owner, DynamicVars["Time"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Block"].UpgradeValueBy(3m);
        DynamicVars["SlowBlock"].UpgradeValueBy(3m);
    }
}
