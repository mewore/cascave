using Godot;

public class WaterParticle : Sprite
{
    private const float ACCELERATION = 1500f;
    private const float POSITION_RANDOMIZATION = 30f;
    private const float LIFETIME = .5f;

    private Vector2 motion = Vector2.Zero;
    public Node2D player;
    private Vector2 sourcePosition;

    private bool returning = false;
    private float lifetime = LIFETIME;

    public override void _Ready()
    {
        sourcePosition = GlobalPosition;
    }

    public override void _Process(float delta)
    {
        Vector2 targetPosition = returning ? sourcePosition : player.GlobalPosition + new Vector2(GD.Randf() - .5f, GD.Randf() - .5f) * POSITION_RANDOMIZATION;
        Vector2 positionDifference = targetPosition - GlobalPosition;
        motion += new Vector2(getAccelerationComponent(motion.x, positionDifference.x), getAccelerationComponent(motion.y, positionDifference.y)) * delta;
        Position += motion * delta;

        if (returning)
        {
            if ((lifetime -= delta) < 0f)
            {
                QueueFree();
            }
            SelfModulate = new Color(SelfModulate, lifetime / LIFETIME);
        }
    }

    public void Release() => returning = true;

    private static float getAccelerationComponent(float motion, float positionDifference)
    {
        var accelerate = motion > 0 ? ACCELERATION : -ACCELERATION;
        var motionSign = Mathf.Sign(motion);
        if (motionSign == 0)
        {
            motionSign = Mathf.Sign(positionDifference);
        }
        return (motionSign == Mathf.Sign(positionDifference) && motion * motion / ACCELERATION < Mathf.Abs(positionDifference))
            ? accelerate
            : -accelerate;
    }
}
