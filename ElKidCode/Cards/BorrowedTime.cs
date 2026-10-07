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

/// <summary>Whenever you lose HP on your turn, gain Time.</summary>
public class BorrowedTime() : ElKidCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [TimeResource.Tip];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StolenSecondsPower>(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<StolenSecondsPower>(choiceContext, Owner.Creature, DynamicVars["StolenSecondsPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
