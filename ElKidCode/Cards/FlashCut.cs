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

/// <summary>FAST: hit and draw. SLOW: hit and bank Time.</summary>
public class FlashCut() : ElKidDualCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar("Damage", 9m, ValueProp.Move), new DamageVar("SlowDamage", 6m, ValueProp.Move), new CardsVar(1), new DynamicVar("Time", 1m)];

    protected override async Task OnPlayFast(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars["Damage"].BaseValue).FromCard(this, cardPlay).Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].BaseValue, Owner);
    }

    protected override async Task OnPlaySlow(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars["SlowDamage"].BaseValue).FromCard(this, cardPlay).Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        TimeResource.Gain(Owner, DynamicVars["Time"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Damage"].UpgradeValueBy(3m);
        DynamicVars["SlowDamage"].UpgradeValueBy(3m);
    }
}
