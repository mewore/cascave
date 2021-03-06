using System;
using System.Collections.Generic;
using Godot;

public class Level : Node2D
{
    private const string MAIN_MENU_PATH = "res://scenes/MainMenu.tscn";

    private Overlay overlay;
    private int currentLevel;
    bool paused = false;
    string targetScene;

    private PauseMenu pauseMenu;
    private Player player;
    private Func<int> firesLeft = () => 1;
    private Vector2 bottomRightLimit;
    private Vector2 winBottomRightLimit;
    private CanvasLayer waterIndicatorLayer;
    private WaterIndicator waterIndicator;
    private CanvasItem canvasModulate;
    private CanvasItem backgroundCanvasModulate;

    private AudioStreamPlayer2D splashInSound;
    private float splashInSoundVolumeDb;
    private AudioStreamPlayer2D splashOutSound;
    private float splashOutSoundVolumeDb;

    [Export]
    private Texture cursorTexture = null;

    [Export]
    private PackedScene waterSplashScene = null;
    public Node waterSplashContainer;

    private bool hasWon = false;

    private float elapsedTime = 0f;

    public override void _Ready()
    {
        overlay = GetNode<Overlay>("Overlay");
        pauseMenu = GetNode<PauseMenu>("PauseUi/PauseMenu");
        overlay.FadeIn();
        currentLevel = Global.CurrentLevel;
        GlobalSound.GetInstance(this).MusicForeground = true;

        var gameNode = GetNode<Node>("Game");
        splashInSound = gameNode.GetNode<AudioStreamPlayer2D>("SplashInSound");
        splashInSoundVolumeDb = splashInSound.VolumeDb;
        splashOutSound = gameNode.GetNode<AudioStreamPlayer2D>("SplashOutSound");
        splashOutSoundVolumeDb = splashOutSound.VolumeDb;

        player = gameNode.GetNode<Player>("Player");
        player.BottomRightLimit = bottomRightLimit = gameNode.GetNode<Node2D>("PlayerLimit").GlobalPosition;
        winBottomRightLimit = new Vector2(bottomRightLimit.x + 1000f, bottomRightLimit.y);
        List<Fire> fires = new List<Fire>();
        foreach (var fire in GetTree().GetNodesInGroup("fire"))
        {
            fires.Add(fire as Fire);
        }
        firesLeft = () =>
        {
            int count = fires.Count;
            foreach (var fire in fires)
            {
                if (fire.IsExtinguished)
                {
                    --count;
                }
            }
            return count;
        };

        var camera = GetNode<Camera2D>("MouseCamera");
        camera.LimitRight = (int)bottomRightLimit.x;
        camera.LimitBottom = (int)bottomRightLimit.y;

        waterIndicatorLayer = GetNode<CanvasLayer>("WaterIndicatorUi");
        waterIndicator = waterIndicatorLayer.GetNode<WaterIndicator>("WaterIndicator");
        waterIndicator.Player = player;

        SetCursor(true);

        waterSplashContainer = GetNode<Node>("Game/WaterSplashes");

        canvasModulate = gameNode.GetNode<CanvasItem>("CanvasModulate");
        backgroundCanvasModulate = GetNode<CanvasItem>("Background/ParallaxBackground/CanvasModulate");
        ApplyLightingSetting(Global.CurrentLightingSetting);
        Global.SINGLETON.Connect(nameof(Global.NewLightingSetting), this, "ApplyLightingSetting");
    }

    public override void _Process(float delta)
    {
        GetTree().Paused = overlay.Transitioning || paused;
        if (GetTree().Paused)
        {
            AudioServer.SetBusEffectEnabled(0, 0, false);
        }
        waterIndicatorLayer.FollowViewportEnable = !(bool)ProjectSettings.GetSetting(WaterIndicator.FOLLOW_MOUSE_SETTING);
    }

    public override void _PhysicsProcess(float delta)
    {
        if (firesLeft() == 0)
        {
            player.BottomRightLimit = winBottomRightLimit;
            if (player.Position.x > bottomRightLimit.x)
            {
                WinLevel();
            }
        }
        if (!GetTree().Paused)
        {
            elapsedTime += delta;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("debug_clear_level"))
        {
            if (OS.IsDebugBuild())
            {
                WinLevel();
            }
        }
        else if (@event.IsActionPressed("pause"))
        {
            if (!overlay.Transitioning)
            {
                pauseMenu.Visible = paused = !paused;
                pauseMenu.CurrentMenu = MenuType.PAUSE;
                GlobalSound.GetInstance(this).MusicForeground = !paused;
                SetCursor(!pauseMenu.Visible);
            }
        }
    }

    public void ApplyLightingSetting(LightingSetting setting)
    {
        canvasModulate.Visible = backgroundCanvasModulate.Visible = setting != LightingSetting.DISABLED;
    }

    public void _on_Overlay_FadeOutDone()
    {
        pauseMenu.Visible = paused = false;
        if (targetScene != null)
        {
            GetTree().ChangeScene(targetScene);
        }
        else
        {
            GD.PushWarning("No target scene is set. Reloading the current scene as a fallback");
            GetTree().ReloadCurrentScene();
        }
    }

    private void LoseLevel()
    {
        GlobalSound.GetInstance(this).PlayLose();
        overlay.FadeOutReverse();
    }

    private void WinLevel()
    {
        if (hasWon)
        {
            return;
        }
        GlobalSound.GetInstance(this).PlayClearLevel();
        targetScene = Global.WinLevel(currentLevel, Mathf.RoundToInt(elapsedTime * 1000f)) ? Global.CurrentLevelPath : MAIN_MENU_PATH;
        overlay.FadeOut();
    }

    public void _on_PauseMenu_ResumeRequested()
    {
        pauseMenu.Visible = paused = false;
        SetCursor(true);
        GlobalSound.GetInstance(this).MusicForeground = true;
    }

    public void _on_PauseMenu_RestartLevelRequested()
    {
        targetScene = Global.CurrentLevelPath;
        pauseMenu.Editable = false;
        overlay.FadeOutReverse();
    }

    public void _on_PauseMenu_ReturnToMainMenuRequested()
    {
        Global.ReturningToMenu = true;
        targetScene = MAIN_MENU_PATH;
        pauseMenu.Editable = false;
        overlay.FadeOutReverse();
    }

    public void _on_Player_Splash(Vector2 position, Vector2 impactDirection, float intensity, bool isAgainstWaterSurface)
    {
        var splash = waterSplashScene.Instance<WaterSplash>();
        splash.Position = position;
        splash.Direction = (isAgainstWaterSurface ? new Vector2(impactDirection.x, -Mathf.Abs(impactDirection.y)) : impactDirection).Normalized();
        splash.Intensity = intensity;
        waterSplashContainer.AddChild(splash);

        if (isAgainstWaterSurface)
        {
            AudioStreamPlayer2D sound = impactDirection.y < 0 ? splashOutSound : splashInSound;
            sound.Position = position;
            sound.VolumeDb = (impactDirection.y < 0f ? splashOutSoundVolumeDb : splashInSoundVolumeDb) + (float)Math.Log10(intensity);
            sound.Play();
        }
    }

    private void SetCursor(bool isCustom)
    {
        if (isCustom && cursorTexture != null)
        {
            Input.SetCustomMouseCursor(cursorTexture, default, new Vector2(16, 16));
        }
        else
        {
            Input.SetCustomMouseCursor(null);
        }
    }
}
