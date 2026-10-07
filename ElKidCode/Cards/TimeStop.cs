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

/// <summary>Spend all Time: gain that much Energy and draw that many cards.</summary>
public class TimeStop() : ElKidCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [TimeResource.Tip, EnergyHoverTip];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var spent = await TimeResource.SpendAll(Owner, CombatState, this);
        if (spent <= 0) return;
        await PlayerCmd.GainEnergy(spent, Owner);
        await CardPileCmd.Draw(choiceContext, spent, Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
