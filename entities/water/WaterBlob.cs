using Godot;

public class WaterBlob : Sprite
{
    private const float ACCELERATION = 1500f;
    private const float POSITION_RANDOMIZATION = 30f;
    private const float LIFETIME = .5f;

    private const float GRAVITY = 300f;
    private const float LAUNCH_SPEED = 400f;

    private Vector2 motion = Vector2.Zero;
    public Node2D Player;
    private Vector2 sourcePosition;
    public WaterPool WaterPool;

    private bool returning = false;
    private float lifetime = LIFETIME;

    [Export(PropertyHint.Layers2dPhysics)]
    private uint collisionLayer = 1;
    private bool launched = false;
    private Physics2DDirectSpaceState directSpaceState;
    private Physics2DShapeQueryParameters intersectParameters;
    private CircleShape2D detectionShape;


    public override void _Ready()
    {
        sourcePosition = GlobalPosition;
    }

    public override void _Process(float delta)
    {
        if (launched)
        {
            return;
        }
        Vector2 targetPosition = returning ? sourcePosition : Player.GlobalPosition + new Vector2(GD.Randf() - .5f, GD.Randf() - .5f) * POSITION_RANDOMIZATION;
        Vector2 positionDifference = targetPosition - GlobalPosition;
        motion += new Vector2(getAccelerationComponent(motion.x, positionDifference.x), getAccelerationComponent(motion.y, positionDifference.y)) * delta;
        Position += motion * delta;

        if (returning)
        {
            if ((lifetime -= delta) < 0f)
            {
                QueueFree();
                WaterPool.ReceiveBlob();
            }
            SelfModulate = new Color(SelfModulate, lifetime / LIFETIME);
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        if (!launched)
        {
            return;
        }
        motion = new Vector2(motion.x, motion.y + GRAVITY * delta);
        var frameMotion = motion * delta;
        intersectParameters.Transform = GlobalTransform;
        intersectParameters.Motion = frameMotion;
        Godot.Collections.Array result = directSpaceState.IntersectShape(intersectParameters, 1);
        if (result.Count > 0)
        {
            QueueFree();
        }
        Position += frameMotion;
    }

    public void Release() => returning = true;

    public void Launch(Vector2 target)
    {
        launched = true;
        directSpaceState = Physics2DServer.SpaceGetDirectState(GetWorld2d().Space);
        detectionShape = new CircleShape2D();
        detectionShape.Radius = GetNode<Node2D>("Radius").Position.Length();
        intersectParameters = new Physics2DShapeQueryParameters();
        intersectParameters.ShapeRid = detectionShape.GetRid();
        intersectParameters.CollisionLayer = collisionLayer;
        intersectParameters.CollideWithAreas = false;
        intersectParameters.CollideWithBodies = true;
        motion = (target - GlobalPosition).Normalized() * LAUNCH_SPEED;
    }

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
