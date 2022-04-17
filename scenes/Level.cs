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

    private CanvasItem pauseMenu;
    private Player player;
    private Func<int> firesLeft = () => 1;
    private Vector2 bottomRightLimit;
    private Vector2 winBottomRightLimit;

    public override void _Ready()
    {
        overlay = GetNode<Overlay>("Overlay");
        pauseMenu = GetNode<CanvasItem>("PauseUi/PauseMenu");
        overlay.FadeIn();
        currentLevel = Global.CurrentLevel;
        GlobalSound.GetInstance(this).MusicForeground = true;

        var gameNode = GetNode<Node>("Game");
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
    }

    public override void _Process(float delta)
    {
        GetTree().Paused = overlay.Transitioning || paused;
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
                GlobalSound.GetInstance(this).MusicForeground = !paused;
            }
        }
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
        GlobalSound.GetInstance(this).PlayClearLevel();
        targetScene = Global.WinLevel(currentLevel) ? Global.CurrentLevelPath : MAIN_MENU_PATH;
        overlay.FadeOut();
    }

    public void _on_Resume_pressed()
    {
        pauseMenu.Visible = paused = false;
        GlobalSound.GetInstance(this).MusicForeground = true;
    }

    public void _on_RestartLevel_pressed()
    {
        targetScene = Global.CurrentLevelPath;
        overlay.FadeOutReverse();
    }

    public void _on_ReturnToMenu_pressed()
    {
        Global.ReturningToMenu = true;
        targetScene = MAIN_MENU_PATH;
        overlay.FadeOutReverse();
    }
}
