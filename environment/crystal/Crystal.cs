using Godot;

public class Crystal : Sprite
{
    private Light2D light;
    private Sprite lightSprite;

    public override void _Ready()
    {
        Frame = (int)(GD.Randi() % Hframes);
        if (GD.Randf() > .5f)
        {
            Scale = new Vector2(-1f, 1f);
        }
        light = GetNode<Light2D>("Light2D");
        lightSprite = GetNode<Sprite>("LightSprite");
        ApplyLightingSetting(Global.CurrentLightingSetting);
        Global.SINGLETON.Connect(nameof(Global.NewLightingSetting), this, "ApplyLightingSetting");
    }

    public void ApplyLightingSetting(LightingSetting setting)
    {
        light.Visible = setting == LightingSetting.NO_SHADOWS || setting == LightingSetting.WITH_SHADOWS;
        lightSprite.Visible = setting == LightingSetting.SPRITES;
    }
}
