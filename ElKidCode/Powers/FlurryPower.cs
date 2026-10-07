using ElKid.ElKidCode.Time;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ElKid.ElKidCode.Powers;

/// <summary>Whenever you play a card Fast, deal damage to a random enemy (targeting like the base game's Juggernaut).</summary>
public class FlurryPower : ElKidPower, IOnFastPlay
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [TimeResource.FastModeTip];

    public async Task AfterFastPlay(PlayerChoiceContext choiceContext, CardModel card)
    {
        var enemies = CombatState.HittableEnemies;
        if (enemies.Count == 0 || Owner.Player == null) return;

        var target = Owner.Player.RunState.Rng.CombatTargets.NextItem(enemies);
        if (target == null) return;
        Flash();
        await CreatureCmd.Damage(choiceContext, target, Amount, ValueProp.Unpowered, Owner);
    }
}
