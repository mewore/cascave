using Godot;

public class PauseMenu : Node2D
{
    [Signal]
    delegate void ResumeRequested();

    [Signal]
    delegate void RestartLevelRequested();

    [Signal]
    delegate void ReturnToMainMenuRequested();

    private CanvasItem pauseMenu;
    private Button resumeButton;
    private Button restartButton;
    private Button settingsButton;
    private Button mainMenuButton;

    private SettingsMenu settingsMenu;

    public bool Editable
    {
        set
        {
            resumeButton.Disabled = restartButton.Disabled = settingsButton.Disabled = mainMenuButton.Disabled = !value;
            settingsMenu.Editable = value;
        }
    }

    public MenuType CurrentMenu
    {
        set
        {
            pauseMenu.Visible = value == MenuType.PAUSE;
            settingsMenu.Visible = value == MenuType.SETTINGS;
        }
    }

    public override void _Ready()
    {
        var container = GetNode<Node>("Container");
        pauseMenu = container.GetNode<CanvasItem>("PauseMenu");
        resumeButton = pauseMenu.GetNode<Button>("Resume");
        restartButton = pauseMenu.GetNode<Button>("RestartLevel");
        settingsButton = pauseMenu.GetNode<Button>("Settings");
        mainMenuButton = pauseMenu.GetNode<Button>("ReturnToMenu");

        settingsMenu = container.GetNode<SettingsMenu>("SettingsMenu");
    }

    public void _on_Resume_pressed() => EmitSignal(nameof(ResumeRequested));

    public void _on_RestartLevel_pressed() => EmitSignal(nameof(RestartLevelRequested));

    public void _on_ReturnToMenu_pressed() => EmitSignal(nameof(ReturnToMainMenuRequested));

    public void _on_Settings_pressed() => CurrentMenu = MenuType.SETTINGS;

    public void _on_SettingsDoneButton_pressed() => CurrentMenu = MenuType.PAUSE;
}
