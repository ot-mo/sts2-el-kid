using ElKid.ElKidCode.Time;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace ElKid.ElKidCode.Cards;

/// <summary>Fast: draw. Slow: bank Time.</summary>
public class WindUp() : ElKidDualCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2), new DynamicVar("Time", 2m)];

    protected override async Task OnPlayFast(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override Task OnPlaySlow(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TimeResource.Gain(Owner, DynamicVars["Time"].IntValue);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
        DynamicVars["Time"].UpgradeValueBy(1m);
    }
}
