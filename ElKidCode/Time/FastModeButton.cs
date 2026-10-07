using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;

namespace ElKid.ElKidCode.Time;

/// <summary>
/// The Fast/Slow toggle shown next to the energy counter for El Kid.
/// Prototype: the mode is local state and is not synced in multiplayer yet.
/// </summary>
public static class FastModeButton
{
    private static readonly Color SlowColor = new("8a8f99");

    public static Control? Create(NCombatUi ui, Player player, CombatState combatState)
    {
        if (player.Character is not Character.ElKid) return null;
        if (player.PlayerCombatState == null) return null;

        var time = CustomResources<TimeResource>.Get(player.PlayerCombatState);

        var button = new Button
        {
            CustomMinimumSize = new Vector2(76, 30),
            Size = new Vector2(76, 30),
            FocusMode = Control.FocusModeEnum.None,
            TooltipText = "Toggle Fast mode (each dual card played in Fast mode spends 1 Time)",
        };
        button.AddThemeFontSizeOverride("font_size", 16);

        button.Pressed += () =>
        {
            if (!IsPlayPhase()) return;
            time.SetFast(!time.IsFast);
        };

        void UpdateLook()
        {
            if (!GodotObject.IsInstanceValid(button)) return;
            button.Text = time.IsFast ? "FAST" : "SLOW";
            var color = time.IsFast ? TimeResource.TimeColor : SlowColor;
            button.AddThemeColorOverride("font_color", color);
            button.AddThemeColorOverride("font_hover_color", color);
            button.AddThemeColorOverride("font_pressed_color", color);
            button.Disabled = !time.IsFast && time.Amount <= 0;
        }

        Action modeChanged = UpdateLook;
        Action<int, int> amountChanged = (_, _) => UpdateLook();
        time.ModeChanged += modeChanged;
        time.AmountChanged += amountChanged;
        button.TreeExiting += () =>
        {
            time.ModeChanged -= modeChanged;
            time.AmountChanged -= amountChanged;
        };

        UpdateLook();
        ui.AddChild(button);
        return button;
    }

    public static bool IsPlayPhase()
    {
        return RunManager.Instance.ActionQueueSynchronizer.CombatState == ActionSynchronizerCombatState.PlayPhase;
    }
}
