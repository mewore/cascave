using Godot;

public class WaterIndicator : Node2D
{
    public const string FOLLOW_MOUSE_SETTING = "application/game/water_indicator_follows_mouse";

    private static readonly Vector2 OFFSET_FROM_PLAYER = Vector2.Up * 40f;

    private Sprite sprite;
    public Player Player;

    private bool ShouldFollowMouse => (bool)ProjectSettings.GetSetting(FOLLOW_MOUSE_SETTING);

    public override void _Ready()
    {
        sprite = GetNode<Sprite>("Sprite");
    }

    public override void _Process(float delta)
    {
        Position = Player == null || ShouldFollowMouse ? GetGlobalMousePosition() : (Player.GlobalPosition + OFFSET_FROM_PLAYER);
        if (Player != null)
        {
            sprite.Frame = Player.GetWaterParts(sprite.Vframes - 1);
            sprite.Visible = Player.IsCarryingWater;
        }
    }
}
