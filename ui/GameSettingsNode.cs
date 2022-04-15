using Godot;
using System;

public class GameSettingsNode : VBoxContainer
{
    private Label retainWaterLabel;

    public override void _Ready()
    {
        retainWaterLabel = GetNode<Label>("RetainWater/RetainWaterLabel");
        UpdateRetainWaterLabel();
    }

    public void UpdateRetainWaterLabel()
    {
        retainWaterLabel.Text = (Player.ShouldRetainWater ? "Press [R]" : "Release the [right mouse button]") +
            "\nto release the water back into its source.";
    }

    public void _on_RetainWaterButton_toggled(bool newValue)
    {
        ProjectSettings.SetSetting(Player.RETAIN_WATER_SETTING, newValue);
        UpdateRetainWaterLabel();
    }
}
