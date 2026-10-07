using BaseLib.Abstracts;
using BaseLib.BaseLibScenes;
using BaseLib.Extensions;
using BaseLib.Patches.UI;
using ElKid.ElKidCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ElKid.ElKidCode.Time;

/// <summary>
/// El Kid's Time resource. Persists between turns (capped at <see cref="Cap"/>), resets each combat.
/// Also holds the Fast/Slow mode: Fast can only be on while there is Time, and turns off when Time runs out.
/// BaseLib registers this automatically because it subclasses CustomResource.
/// </summary>
public class TimeResource() : BasicCustomResource(ResourceId)
{
    public const string ResourceId = "ELKID-TIME";
    public const int Cap = 5;

    public static readonly Color TimeColor = new("5fd3ff");
    private static readonly Color NumberOutlineColor = new("08131f");

    public override string TexturePath => "charui/time.png".ImagePath();
    public override Color MainColor => NumberOutlineColor;

    public bool IsFast { get; private set; }

    /// <summary>Fired when the mode changes, so the button and cards can update.</summary>
    public event Action? ModeChanged;

    public override int Amount
    {
        get => base.Amount;
        set
        {
            var before = base.Amount;
            base.Amount = Math.Clamp(value, 0, Cap);
            if (base.Amount == 0) SetFast(false);
            if (base.Amount != before) RefreshCards();
        }
    }

    public override bool ShouldShowDisplay() => Owner?.Character is Character.ElKid;

    public void SetFast(bool fast)
    {
        if (fast && Amount <= 0) fast = false;
        if (fast == IsFast) return;

        IsFast = fast;
        ModeChanged?.Invoke();
        RefreshCards();
    }

    /// <summary>Makes cards re-render so dual-mode cards show the active half.</summary>
    private void RefreshCards()
    {
        var combatState = Owner?.PlayerCombatState;
        if (combatState == null) return;
        foreach (var card in combatState.AllCards)
            card.InvokeEnergyCostChanged();
    }

    public static TimeResource? For(Player? player)
    {
        return CustomResources<TimeResource>.TryGet(player?.PlayerCombatState, out var time) ? time : null;
    }

    public static void Gain(Player player, int amount)
    {
        For(player)?.ModifyAmount(amount);
    }

    /// <summary>Spends all of a player's Time and returns how much was spent.</summary>
    public static async Task<int> SpendAll(Player player, ICombatState? combatState, AbstractModel? spender)
    {
        var time = For(player);
        if (time == null || combatState == null || time.Amount <= 0) return 0;

        var amount = time.Amount;
        return await time.Spend<TimeResource>(combatState, spender, amount, optional: true) ? amount : 0;
    }

    public static IHoverTip Tip => new TimeResource().MakeTip()!;

    public static IHoverTip FastModeTip => new HoverTip(
        new LocString("static_hover_tips", "ELKID-FAST_MODE.title"),
        new LocString("static_hover_tips", "ELKID-FAST_MODE.description"),
        null);

    public override void RegisterResourceVisuals<T>()
    {
        // Replaces BasicCustomResource's default 88px counter with a smaller, higher-contrast one.
        // (No card-cost visuals are registered, as no card uses Time as a cost.)
        ValidateType<T>();
        ExtraCombatUi.RegisterCombatUiElement(CreateCounter<T>, ExtraCombatUi.CombatUiPositioning.AroundEnergy);
        ExtraCombatUi.RegisterCombatUiElement(FastModeButton.Create, ExtraCombatUi.CombatUiPositioning.AroundEnergy);
    }

    private const float CounterSize = 60f;
    private const int CounterFontSize = 28;

    private static Control? CreateCounter<T>(NCombatUi ui, Player player, CombatState combatState)
        where T : CustomResource, new()
    {
        if (player.PlayerCombatState == null) return null;
        if (CustomResources<T>.Get(player.PlayerCombatState) is not TimeResource time) return null;

        var texture = PreloadManager.Cache.GetTexture2D(time.TexturePath);
        var display = NAdditionalResourceDisplay.Create<T>(player, time, texture);
        display.Size = new Vector2(CounterSize, CounterSize);

        if (display.GetNodeOrNull<Control>("CountLabel") is { } label)
        {
            label.AddThemeFontSizeOverrideAll(CounterFontSize);
            label.AddThemeConstantOverride("outline_size", 10);
            label.AddThemeColorOverride("font_outline_color", NumberOutlineColor);
            label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.5f));
        }

        ui.AddChild(display);
        return display;
    }
}
