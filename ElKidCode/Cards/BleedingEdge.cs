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

/// <summary>Lose HP, then FAST: a big hit, or SLOW: a hit and lots of Time.</summary>
public class BleedingEdge() : ElKidDualCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new HpLossVar(2m), new DamageVar("Damage", 13m, ValueProp.Move), new DamageVar("SlowDamage", 7m, ValueProp.Move), new DynamicVar("Time", 2m)];

    protected override async Task OnPlayFast(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ElKidCmd.LoseHp(choiceContext, Owner, DynamicVars["HpLoss"].BaseValue, this, cardPlay);
        await DamageCmd.Attack(DynamicVars["Damage"].BaseValue).FromCard(this, cardPlay).Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override async Task OnPlaySlow(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ElKidCmd.LoseHp(choiceContext, Owner, DynamicVars["HpLoss"].BaseValue, this, cardPlay);
        await DamageCmd.Attack(DynamicVars["SlowDamage"].BaseValue).FromCard(this, cardPlay).Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        TimeResource.Gain(Owner, DynamicVars["Time"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Damage"].UpgradeValueBy(4m);
        DynamicVars["SlowDamage"].UpgradeValueBy(3m);
    }
}
