using Godot;

public class GameSettingsNode : VBoxContainer
{
    private Label retainWaterLabel;

    private Button retainWaterButton;
    private Button waterIndicatorFollowsMouseButton;
    private Button fireLightFlickersButton;
    private OptionButton lightingDropdown;

    public bool Editable
    {
        set
        {
            retainWaterButton.Disabled = waterIndicatorFollowsMouseButton.Disabled = fireLightFlickersButton.Disabled = lightingDropdown.Disabled = !value;
        }
    }


    public override void _Ready()
    {
        retainWaterButton = GetNode<Button>("RetainWater/RetainWaterButton");
        retainWaterLabel = GetNode<Label>("RetainWater/RetainWaterLabel");
        retainWaterButton.Pressed = (bool)ProjectSettings.GetSetting(Player.RETAIN_WATER_SETTING);
        UpdateRetainWaterLabel();

        waterIndicatorFollowsMouseButton = GetNode<Button>("WaterIndicatorFollowsMouseButton");
        waterIndicatorFollowsMouseButton.Pressed = (bool)ProjectSettings.GetSetting(WaterIndicator.FOLLOW_MOUSE_SETTING);

        fireLightFlickersButton = GetNode<Button>("FireLightFlickersButton");
        fireLightFlickersButton.Pressed = (bool)ProjectSettings.GetSetting(Fire.LIGHT_FLICKERS_SETTING);

        lightingDropdown = GetNode<OptionButton>("Lighting/LightingDropdown");
        lightingDropdown.AddItem("Disabled", (int)LightingSetting.DISABLED);
        lightingDropdown.AddItem("Sprites", (int)LightingSetting.SPRITES);
        lightingDropdown.AddItem("No shadows", (int)LightingSetting.NO_SHADOWS);
        lightingDropdown.AddItem("With shadows", (int)LightingSetting.WITH_SHADOWS);
        lightingDropdown.Select((int)Global.CurrentLightingSetting);
    }

    public void UpdateRetainWaterLabel()
    {
        retainWaterLabel.Text = (Player.ShouldRetainWater ? "Press [R]" : "Release the [right mouse button]") +
            " to release the water back into its source.";
    }

    public void _on_RetainWaterButton_toggled(bool newValue)
    {
        ProjectSettings.SetSetting(Player.RETAIN_WATER_SETTING, newValue);
        UpdateRetainWaterLabel();
    }

    public void _on_WaterIndicatorFollowsMouseButton_toggled(bool newValue)
    {
        ProjectSettings.SetSetting(WaterIndicator.FOLLOW_MOUSE_SETTING, newValue);
    }

    public void _on_FireLightFlickersButton_toggled(bool newValue)
    {
        ProjectSettings.SetSetting(Fire.LIGHT_FLICKERS_SETTING, newValue);
    }

    public void _on_LightingDropdown_item_selected(int index)
    {
        Global.CurrentLightingSetting = (LightingSetting)index;
    }
}
