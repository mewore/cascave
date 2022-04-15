using System.Collections.Generic;
using Godot;

public class Player : KinematicBody2D
{
    public const string RETAIN_WATER_SETTING = "application/game/player_retains_water";
    private const int MAX_WATER_BLOBS = 50;

    private const float ACCELERATION = 1600f;
    private const float AIR_ACCELERATION = 400f;
    private const float UNDERWATER_ACCELERATION = 500f;
    private const float INACTIVE_ACCELERATION = 100f;
    private const float MAX_SPEED = 120f;
    private const float MAX_UNDERWATER_SPEED = 120f;
    private const float UNDETWATER_DAMPING = .05f;

    private const float MAX_FALL_SPEED = 300f;
    private const float GRAVITY = 600f;
    private const float UNDERWATER_GRAVITY = 200f;

    private float now = 0f;

    private float jumpSpeed;
    private const float JUMP_SPEED_RETENTION = .5f;
    private const float JUMP_GRACE_PERIOD = .2f;
    private float lastWantedToJumpAt = -JUMP_GRACE_PERIOD * 2f;
    private float lastOnFloorAt = 0f;
    private bool isJumping = false;

    private const int MIN_DASH_COST = 5;
    private const int MAX_DASH_COST = 10;
    private float minDashSpeed;
    private float maxDashSpeed;

    private const float LAUNCH_COOLDOWN = .05f;
    private float canLaunchAt = 0f;

    private Vector2 motion = new Vector2();
    public Vector2 Motion { get => motion; }

    private Sprite sprite;
    private Sprite outlineSprite;
    private AnimationPlayer animationPlayer;

    private AudioStreamPlayer jumpSound;
    private AudioStreamPlayer landSound;

    private Node2D center;

    [Export(PropertyHint.Layers2dPhysics)]
    private uint waterLayer = 0;
    private Physics2DDirectSpaceState directSpaceState;

    [Export]
    private PackedScene waterParticleScene = null;
    private readonly Stack<WaterBlob> waterBlobs = new Stack<WaterBlob>(MAX_WATER_BLOBS);
    private Physics2DShapeQueryParameters waterIntersectParameters;
    private Resource waterDetectionShape;

    [Export]
    private NodePath waterBlobContainer = null;
    private Node waterBlobContainerNode;

    private Vector2 lastDesiredMotion;

    [Export]
    private string inputSuffix = "";

    [Export]
    private string walkLeftInput = "move_left";

    [Export]
    private string walkRightInput = "move_right";

    [Export]
    private string jumpInput = "jump";

    private float DesiredHorizontalMovement => Input.GetActionStrength(walkRightInput) - Input.GetActionStrength(walkLeftInput);
    private float DesiredVerticalMovement => Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up");
    public static bool ShouldRetainWater => (bool)ProjectSettings.GetSetting(RETAIN_WATER_SETTING);

    public override void _Ready()
    {
        float jumpHeight = -GetNode<Node2D>("MaxJumpHeight").Position.y;
        jumpSpeed = Mathf.Sqrt(GRAVITY * jumpHeight * 2f);
        float minDashHeight = -GetNode<Node2D>("MinDashHeight").Position.y;
        minDashSpeed = Mathf.Sqrt(GRAVITY * minDashHeight * 2f);
        float maxDashHeight = -GetNode<Node2D>("MaxDashHeight").Position.y;
        maxDashSpeed = Mathf.Sqrt(GRAVITY * maxDashHeight * 2f);
        sprite = GetNode<Sprite>("Sprite");
        outlineSprite = GetNode<Sprite>("Sprite/Outline");
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer"); ;
        jumpSound = GetNode<AudioStreamPlayer>("JumpSound");
        landSound = GetNode<AudioStreamPlayer>("LandSound");
        if (!inputSuffix.Equals(""))
        {
            walkLeftInput += "_" + inputSuffix;
            walkRightInput += "_" + inputSuffix;
            jumpInput += "_" + inputSuffix;
        }
        center = GetNode<Node2D>("Center");

        var waterDetectionShapeNode = GetNode<CollisionShape2D>("WaterDetectionShape");
        directSpaceState = Physics2DServer.SpaceGetDirectState(GetWorld2d().Space);
        waterIntersectParameters = new Physics2DShapeQueryParameters();
        waterIntersectParameters.SetShape(waterDetectionShape = waterDetectionShapeNode.Shape);
        waterDetectionShape.Reference_();
        waterIntersectParameters.CollisionLayer = waterLayer;
        waterIntersectParameters.CollideWithBodies = false;
        waterIntersectParameters.CollideWithAreas = true;
        if (waterBlobContainer != null)
        {
            waterBlobContainerNode = GetNode<Node>(waterBlobContainer);
        }
        waterDetectionShapeNode.QueueFree();
    }

    public override void _PhysicsProcess(float delta)
    {
        now += delta;
    }

    public override void _Process(float delta)
    {
        outlineSprite.Frame = sprite.Frame;
    }

    public override void _ExitTree()
    {
        waterDetectionShape.Unreference();
    }

    private bool IsUnderwater() => directSpaceState.IntersectPoint(GlobalPosition + center.Position, 1, null, waterLayer, false, true).Count > 0;

    public void Move(float delta, bool canControl)
    {
        bool isUnderwater = IsUnderwater();
        bool isOnFloor = !isUnderwater && IsOnFloor();
        float lastMotionY = motion.y;

        motion.y = Mathf.Min(isOnFloor ? motion.y : (motion.y + (isUnderwater ? UNDERWATER_GRAVITY : GRAVITY) * delta), MAX_FALL_SPEED);
        if (isUnderwater)
        {
            if (canControl)
            {
                motion += new Vector2(DesiredHorizontalMovement, DesiredVerticalMovement).Normalized() * UNDERWATER_ACCELERATION * delta;
            }
            motion *= (1f - UNDETWATER_DAMPING);
        }
        else
        {
            float maxAcceleration = (canControl ? (isOnFloor ? ACCELERATION : AIR_ACCELERATION) : INACTIVE_ACCELERATION) * delta;
            float targetMotionX = canControl
                ? (Input.GetActionStrength(walkRightInput) - Input.GetActionStrength(walkLeftInput)) * MAX_SPEED
                : 0;
            motion.x = Mathf.Abs(targetMotionX - motion.x) <= maxAcceleration
                ? targetMotionX
                : motion.x + (targetMotionX > motion.x ? maxAcceleration : -maxAcceleration);
        }

        if (Input.IsActionJustPressed(jumpInput))
        {
            lastWantedToJumpAt = now;
        }
        if (isOnFloor)
        {
            lastOnFloorAt = now;
        }

        if (Input.IsActionJustPressed("dash"))
        {
            Dash();
        }

        if (canControl)
        {
            if (!isJumping && Mathf.Max(now - lastWantedToJumpAt, now - lastOnFloorAt) <= JUMP_GRACE_PERIOD)
            {
                lastOnFloorAt = lastWantedToJumpAt = now - JUMP_GRACE_PERIOD;
                motion.y = -jumpSpeed;
                isJumping = true;
                jumpSound.PitchScale = 1f + (GD.Randf() - .5f) * .5f;
                jumpSound.Play();
            }
            else if (isJumping && Input.IsActionJustReleased(jumpInput))
            {
                motion = new Vector2(motion.x, motion.y * JUMP_SPEED_RETENTION);
                isJumping = false;
            }
        }
        isJumping = isJumping && motion.y < 0f;

        motion = MoveAndSlide(motion, Vector2.Up, true);

        if (!isUnderwater && canControl && !isOnFloor && IsOnFloor() && lastMotionY > jumpSpeed * .5f)
        {
            landSound.Play();
        }

        if (canControl && sprite.Visible)
        {
            string targetAnimation = (Mathf.Abs(motion.x) < MAX_SPEED * .2f) ? "idle" : "walk";
            if (now - lastOnFloorAt > JUMP_GRACE_PERIOD)
            {
                targetAnimation = ((motion.y < 0f) ? "jump" : "fall") + (targetAnimation.Equals("idle") ? "" : "_side");
            }
            int motionScale = Mathf.Sign(motion.x);
            if (motionScale != 0 && Mathf.Sign(sprite.Scale.x) != motionScale)
            {
                sprite.Scale = new Vector2(sprite.Scale.x * -1f, sprite.Scale.y);
            }
            if (!targetAnimation.Equals(animationPlayer.CurrentAnimation))
            {
                animationPlayer.Play(targetAnimation);
            }
        }
    }

    public WaterPool GetDetectedPool(Vector2 position)
    {
        waterIntersectParameters.Transform = new Transform2D(Vector2.Right, Vector2.Down, position);
        Godot.Collections.Array results = directSpaceState.IntersectShape(waterIntersectParameters, 1);
        return results.Count > 0 ? ((results[0] as Godot.Collections.Dictionary)["collider"] as WaterPool) : null;
    }

    private void Dash()
    {
        Vector2 desiredMotion = new Vector2(DesiredHorizontalMovement, DesiredVerticalMovement);
        if (desiredMotion.LengthSquared() > .001f)
        {
            lastDesiredMotion = desiredMotion;
        }
        if (waterBlobs.Count < MIN_DASH_COST || lastDesiredMotion == null)
        {
            return;
        }

        int usedBlobs = Mathf.Min(MAX_DASH_COST, waterBlobs.Count);
        motion = lastDesiredMotion.Normalized() * Mathf.Lerp(minDashSpeed, maxDashSpeed, (float)usedBlobs / MAX_DASH_COST);
        for (int blobIndex = 0; blobIndex < usedBlobs; blobIndex++)
        {
            waterBlobs.Pop().QueueFree();
        }
    }

    public void HandleWater()
    {
        if (Input.IsActionPressed("charge"))
        {
            if (waterBlobs.Count < MAX_WATER_BLOBS && waterBlobContainerNode != null && waterParticleScene != null)
            {
                Vector2 mousePosition = GetGlobalMousePosition();
                WaterPool detectedPool = GetDetectedPool(mousePosition);
                if (detectedPool != null && detectedPool.TryToTakeBlob())
                {
                    var waterBlob = waterParticleScene.Instance<WaterBlob>();
                    waterBlob.Position = detectedPool.GetRandomPosition();
                    waterBlob.Player = center;
                    waterBlobContainerNode.AddChild(waterBlob);
                    waterBlobs.Push(waterBlob);
                    waterBlob.WaterPool = detectedPool;
                }
            }
        }
        else if (!ShouldRetainWater || Input.IsActionJustPressed("release_water"))
        {
            foreach (WaterBlob blob in waterBlobs)
            {
                blob.Release();
            }
            waterBlobs.Clear();
        }

        if (Input.IsActionPressed("launch") && waterBlobs.Count > 0 && now >= canLaunchAt)
        {
            canLaunchAt = now + LAUNCH_COOLDOWN;
            waterBlobs.Pop().Launch(GetGlobalMousePosition());
        }
    }
}
