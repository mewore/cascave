using Godot;

public class Crystal : Sprite
{
    public override void _Ready()
    {
        Frame = (int)(GD.Randi() % Hframes);
        if (GD.Randf() > .5f)
        {
            Scale = new Vector2(-1f, 1f);
        }
    }
}
