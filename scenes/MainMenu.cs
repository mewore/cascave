using System;
using Godot;

public class MainMenu : Node2D
{
    private OptionButton defaultDifficultyDropdown;
    private Button playButton;
    private Button continueButton;
    private Button levelsButton;
    private Button settingsButton;
    private Overlay overlay;

    private CanvasItem credits;
    private CanvasItem mainMenu;
    private CanvasItem levelsMenu;
    private SettingsMenu settingsMenu;

    private MenuType CurrentMenu
    {
        set
        {
            mainMenu.Visible = credits.Visible = value == MenuType.MAIN;
            levelsMenu.Visible = value == MenuType.LEVELS;
            settingsMenu.Visible = value == MenuType.SETTINGS;
        }
    }

    public override void _Ready()
    {
        credits = GetNode<CanvasItem>("Credits");
        var container = GetNode<Node>("Container");
        mainMenu = container.GetNode<CanvasItem>("MainMenu");
        mainMenu.GetNode<Label>("Title").Text = (string)ProjectSettings.GetSetting("application/config/name");
        defaultDifficultyDropdown = mainMenu.GetNode<OptionButton>("Difficulty/DifficultyDropdown");
        defaultDifficultyDropdown.AddItem("Easy");
        defaultDifficultyDropdown.AddItem("Medium");
        defaultDifficultyDropdown.AddItem("Hard");
        defaultDifficultyDropdown.Select((int)Global.CurrentDefaultDifficultySetting);
        playButton = mainMenu.GetNode<Button>("PlayButton");
        continueButton = mainMenu.GetNode<Button>("ContinueButton");
        levelsButton = mainMenu.GetNode<Button>("LevelsButton");
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
        if (!Global.LoadRecordData())
        {
            continueButton.Disabled = true;
        }
        else
        {
            _on_DifficultyDropdown_item_selected(defaultDifficultyDropdown.Selected);
        }
        mainMenu.GetNode<CanvasItem>("WinLabel").Visible = Global.HasBeatenAllLevels;
        GlobalSound.GetInstance(this).MusicForeground = false;

        settingsMenu = container.GetNode<SettingsMenu>("SettingsMenu");
        Input.SetCustomMouseCursor(null);

        int[] bestLevels = new int[]{
            Global.BestLevelForDifficulty(GameDifficulty.EASY),
            Global.BestLevelForDifficulty(GameDifficulty.MEDIUM),
            Global.BestLevelForDifficulty(GameDifficulty.HARD),
        };
        levelsMenu = container.GetNode<CanvasItem>("LevelsMenu");
        var levelContainer = levelsMenu.GetNode<GridContainer>("GridContainer");
        for (int levelIndex = 0; levelIndex < Global.GetLevelCount(); levelIndex++)
        {
            for (int difficulty = 0; difficulty < 3; difficulty++)
            {
                var singleLevelContainer = new HBoxContainer();
                var button = new Button();
                var label = new Label();
                singleLevelContainer.AddChild(button);
                singleLevelContainer.AddChild(label);

                button.Disabled = levelIndex + 1 > bestLevels[difficulty];
                button.Text = (levelIndex + 1).ToString();
                int bestTime = Global.GetBestLevelTime(levelIndex, (GameDifficulty)difficulty);
                if (bestTime == -1)
                {
                    label.Text = "--:--.---";
                }
                else
                {
                    int milliseconds = bestTime % 1000;
                    bestTime /= 1000;
                    int seconds = bestTime % 60;
                    bestTime /= 60;
                    int minutes = bestTime;
                    label.Text = String.Format("{0}{1}:{2}{3}.{4}{5}{6}", minutes / 10, minutes % 10, seconds / 10, seconds % 10,
                        milliseconds / 100, (milliseconds / 10) % 10, milliseconds % 10);
                }

                if (!button.Disabled)
                {
                    button.Connect("pressed", this, nameof(_on_LevelRequested), new Godot.Collections.Array(levelIndex + 1, difficulty));
                }

                levelContainer.AddChild(singleLevelContainer);
            }
        }
    }

    public void _on_LevelRequested(int levelId, int difficulty)
    {
        Global.Difficulty = (GameDifficulty)difficulty;
        Global.SetLevel(levelId);
        FadeOut();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            CurrentMenu = MenuType.MAIN;
        }
    }

    public void _on_PlayButton_pressed()
    {
        Global.Difficulty = Global.CurrentDefaultDifficultySetting;
        Global.SetLevelToFirst();
        FadeOut();
    }

    public void _on_ContinueButton_pressed()
    {
        Global.Difficulty = Global.CurrentDefaultDifficultySetting;
        Global.SetLevelToBest();
        FadeOut();
    }

    private void FadeOut()
    {
        defaultDifficultyDropdown.Disabled = playButton.Disabled = continueButton.Disabled = levelsButton.Disabled = settingsButton.Disabled = true;
        settingsMenu.Editable = false;
        overlay.FadeOut();
    }

    public void _on_Overlay_FadeOutDone()
    {
        GetTree().ChangeScene(Global.CurrentLevelPath);
    }

    public void _on_SettingsButton_pressed() => CurrentMenu = MenuType.SETTINGS;

    public void _on_SettingsDoneButton_pressed() => CurrentMenu = MenuType.MAIN;

    public void _on_BackButton_pressed() => CurrentMenu = MenuType.MAIN;

    public void _on_LevelsButton_pressed() => CurrentMenu = MenuType.LEVELS;

    public void _on_DifficultyDropdown_item_selected(int index)
    {
        Global.Difficulty = Global.CurrentDefaultDifficultySetting = (GameDifficulty)index;
        continueButton.Disabled = Global.BestLevel <= 1;
        continueButton.Text = continueButton.Disabled ? "Continue" : ("Continue from level " + Global.BestLevel);
    }
}
