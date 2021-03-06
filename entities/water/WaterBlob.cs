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
    private uint collisionMask = 1;
    private bool launched = false;
    private bool following = true;
    private Physics2DDirectSpaceState directSpaceState;
    private Physics2DShapeQueryParameters intersectParameters;
    private CircleShape2D detectionShape;


    public override void _Ready()
    {
        sourcePosition = GlobalPosition;
    }

    public override void _PhysicsProcess(float delta)
    {
        if (launched)
        {
            FlyAsProjectile(delta);
        }
        if (following)
        {
            FollowTarget(delta);
        }
    }

    private void FollowTarget(float delta)
    {
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

    private void FlyAsProjectile(float delta)
    {
        motion = new Vector2(motion.x, motion.y + GRAVITY * delta);
        var frameMotion = motion * delta;
        intersectParameters.Transform = GlobalTransform;
        intersectParameters.Motion = frameMotion;
        Godot.Collections.Array result = directSpaceState.IntersectShape(intersectParameters, 1);
        if (result.Count > 0)
        {
            foreach (Godot.Collections.Dictionary collision in result)
            {
                var colliderOwner = (collision["collider"] as Node).Owner;
                if (!(colliderOwner is Fire))
                {
                    QueueFree();
                    break;
                }
                else if ((colliderOwner as Fire).TakeDamage())
                {
                    GetNode<AudioStreamPlayer2D>("SizzleSound").Play();
                    GetNode<CPUParticles2D>("Steam").Emitting = true;
                    GetNode<CPUParticles2D>("Steam").Direction = motion.Normalized();
                    GetNode<Timer>("Timer").Start();
                    SelfModulate = new Color(0f, 0f, 0f, 0f);
                    launched = false;
                }
            }
        }
        Position += frameMotion;
    }

    public void Release() => returning = true;

    public void Launch(Vector2 target)
    {
        launched = true;
        following = false;
        directSpaceState = Physics2DServer.SpaceGetDirectState(GetWorld2d().Space);
        detectionShape = new CircleShape2D();
        detectionShape.Radius = GetNode<Node2D>("Radius").Position.Length();
        intersectParameters = new Physics2DShapeQueryParameters();
        intersectParameters.ShapeRid = detectionShape.GetRid();
        intersectParameters.CollisionLayer = collisionMask;
        intersectParameters.CollideWithAreas = true;
        intersectParameters.CollideWithBodies = true;
        motion = (target - GlobalPosition).Normalized() * LAUNCH_SPEED;
    }

    private static float getAccelerationComponent(float motion, float positionDifference)
    {
        if (positionDifference < 0f)
        {
            return -getAccelerationComponent(-motion, -positionDifference);
        }
        return motion < 0f || motion * motion / (2 * ACCELERATION) < positionDifference
            ? ACCELERATION
            : -ACCELERATION;
    }

    public void _on_Timer_timeout() => QueueFree();
}
