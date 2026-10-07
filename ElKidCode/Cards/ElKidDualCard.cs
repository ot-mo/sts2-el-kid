using ElKid.ElKidCode.Time;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace ElKid.ElKidCode.Cards;

/// <summary>
/// A card with a Fast half and a Slow half. The half that is played depends on El Kid's Fast mode:
/// in Fast mode the card spends <see cref="FastTimeCost"/> Time and plays its Fast half, otherwise it plays its Slow half.
/// Descriptions receive an "IsFast" flag, so localization can highlight the active half.
/// </summary>
public abstract class ElKidDualCard(int cost, CardType type, CardRarity rarity, TargetType slowTarget, TargetType fastTarget) :
    ElKidCard(cost, type, rarity, slowTarget)
{
    public const int FastTimeCost = 1;

    /// <summary>Set by right-clicking the card in hand: this one card plays Fast even when Fast mode is off.</summary>
    public bool IsPrimed { get; private set; }

    /// <summary>True when this card would currently play its Fast half.</summary>
    protected bool IsFastActive =>
        IsMutable && TimeResource.For(Owner) is { Amount: >= FastTimeCost } time && (time.IsFast || IsPrimed);

    public void TogglePrimed()
    {
        if (!IsPrimed && TimeResource.For(Owner) is not { Amount: >= FastTimeCost }) return;
        IsPrimed = !IsPrimed;
        InvokeEnergyCostChanged();
    }

    public override TargetType TargetType => IsFastActive ? fastTarget : base.TargetType;

    protected override bool ShouldGlowGoldInternal => IsFastActive;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [TimeResource.Tip, TimeResource.FastModeTip];

    protected abstract Task OnPlayFast(PlayerChoiceContext choiceContext, CardPlay cardPlay);

    protected abstract Task OnPlaySlow(PlayerChoiceContext choiceContext, CardPlay cardPlay);

    protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var time = TimeResource.For(Owner);
        var fastNeedsTarget = fastTarget is TargetType.AnyEnemy;
        var canPlayFast = time is { Amount: >= FastTimeCost } && (time.IsFast || IsPrimed)
                          && (!fastNeedsTarget || cardPlay.Target != null);
        IsPrimed = false;

        if (canPlayFast && CombatState != null
                        && await time!.Spend<TimeResource>(CombatState, this, FastTimeCost, optional: true))
        {
            await OnPlayFast(choiceContext, cardPlay);
            foreach (var listener in Owner.Creature.Powers.OfType<IOnFastPlay>().ToList())
                await listener.AfterFastPlay(choiceContext, this);
        }
        else
        {
            await OnPlaySlow(choiceContext, cardPlay);
        }
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add("IsFast", IsFastActive);
    }
}
