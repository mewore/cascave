using Godot;

public class SettingsMenu : VBoxContainer
{
    private SoundControl soundControl;
    private GameSettingsNode gameSettings;

    public bool Editable { set => soundControl.Editable = gameSettings.Editable = value; }

    public override void _Ready()
    {
        soundControl = GetNode<SoundControl>("SoundControl");
        gameSettings = GetNode<GameSettingsNode>("GameSettings");
    }
}
