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

/// <summary>FAST: hit all enemies twice. SLOW: hit all enemies and bank Time.</summary>
public class WhirlingBlade() : ElKidDualCard(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar("Damage", 6m, ValueProp.Move), new DynamicVar("Time", 1m)];

    protected override async Task OnPlayFast(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars["Damage"].BaseValue).WithHitCount(2).FromCard(this, cardPlay).TargetingAllOpponents(CombatState!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override async Task OnPlaySlow(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars["Damage"].BaseValue).FromCard(this, cardPlay).TargetingAllOpponents(CombatState!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        TimeResource.Gain(Owner, DynamicVars["Time"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Damage"].UpgradeValueBy(2m);
    }
}
