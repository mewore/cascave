using Godot;

public class WaterPool : Area2D
{
    private const float BLOB_TAKE_COOLDOWN = .05f;
    private float blobTakeCooldown = 0f;

    private float extentX = 0f;
    private float extentY = 0f;

    public override void _PhysicsProcess(float delta)
    {
        blobTakeCooldown = Mathf.Max(0f, blobTakeCooldown - delta);
        var shape = GetNode<CollisionShape2D>("CollisionShape2D").Shape as RectangleShape2D;
        extentX = shape.Extents.x * 2f;
        extentY = shape.Extents.y * 2f;
    }

    public Vector2 GetRandomPosition()
    {
        return GlobalPosition + new Vector2((GD.Randf() - .5f) * extentX, (GD.Randf() - .5f) * extentY);
    }

    public bool TryToTakeBlob()
    {
        if (blobTakeCooldown > 0f)
        {
            return false;
        }
        blobTakeCooldown = BLOB_TAKE_COOLDOWN;
        return true;
    }
}
