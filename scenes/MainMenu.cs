using Godot;

public class MainMenu : Node2D
{
    private Button playButton;
    private Button continueButton;
    private Button settingsButton;
    private Overlay overlay;

    private CanvasItem mainMenu;
    private SettingsMenu settingsMenu;

    private MenuType CurrentMenu
    {
        set
        {
            mainMenu.Visible = value == MenuType.MAIN;
            settingsMenu.Visible = value == MenuType.SETTINGS;
        }
    }

    public override void _Ready()
    {
        var container = GetNode<Node>("Container");
        mainMenu = container.GetNode<CanvasItem>("MainMenu");
        mainMenu.GetNode<Label>("Title").Text = (string)ProjectSettings.GetSetting("application/config/name");
        playButton = mainMenu.GetNode<Button>("PlayButton");
        continueButton = mainMenu.GetNode<Button>("ContinueButton");
        settingsButton = mainMenu.GetNode<Button>("SettingsButton");
        overlay = GetNode<Overlay>("Overlay");
        if (Global.ReturningToMenu)
        {
            overlay.FadeInReverse();
            Global.ReturningToMenu = false;
        }
        else
        {
            overlay.FadeIn();
        }
        if (!Global.LoadBestLevel())
        {
            continueButton.Disabled = true;
        }
        else
        {
            continueButton.Text += " from level " + Global.BestLevel;
        }
        mainMenu.GetNode<CanvasItem>("WinLabel").Visible = Global.HasBeatenAllLevels;
        GlobalSound.GetInstance(this).MusicForeground = false;

        settingsMenu = container.GetNode<SettingsMenu>("SettingsMenu");
    }

    public void _on_PlayButton_pressed()
    {
        Global.SetLevelToFirst();
        FadeOut();
    }

    public void _on_ContinueButton_pressed()
    {
        Global.SetLevelToBest();
        FadeOut();
    }

    private void FadeOut()
    {
        playButton.Disabled = continueButton.Disabled = settingsButton.Disabled = true;
        settingsMenu.Editable = false;
        overlay.FadeOut();
    }

    public void _on_Overlay_FadeOutDone()
    {
        GetTree().ChangeScene(Global.CurrentLevelPath);
    }

    public void _on_SettingsButton_pressed() => CurrentMenu = MenuType.SETTINGS;

    public void _on_SettingsDoneButton_pressed() => CurrentMenu = MenuType.MAIN;
}
