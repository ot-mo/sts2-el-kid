using ElKid.ElKidCode.Time;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ElKid.ElKidCode.Powers;

/// <summary>Whenever you lose HP on your turn, gain Time (same trigger as the base game's Rupture).</summary>
public class StolenSecondsPower : ElKidPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [TimeResource.Tip];

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && result.UnblockedDamage > 0 && CombatState.CurrentSide == Owner.Side && Owner.Player != null)
        {
            Flash();
            TimeResource.Gain(Owner.Player, Amount);
        }
        return Task.CompletedTask;
    }
}
