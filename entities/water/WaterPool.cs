using System.Collections.Generic;
using Godot;

public class WaterPool : Area2D
{
    private const int PARTICLE_RESOLUTION = 5;
    private const int TOTAL_PARTICLES = 50;
    private const int PARTICLES_PER_EMITTER = TOTAL_PARTICLES / PARTICLE_RESOLUTION;
    private const int BLOB_COUNT = 200;
    private const float BLOB_TAKE_COOLDOWN = .01f;
    private float blobTakeCooldown = 0f;

    private float extentX = 0f;
    private float extentY = 0f;

    private int lastBlobCount = BLOB_COUNT;
    private int blobsLeft = BLOB_COUNT;
    private List<CPUParticles2D> particleEmitters = new List<CPUParticles2D>(PARTICLE_RESOLUTION);

    [Export(PropertyHint.Range, "0, 1")]
    private float saturation = 1f;

    public bool HasBlobsLeft => blobsLeft > 0;

    public override void _Ready()
    {
        if (Global.Difficulty == GameDifficulty.MEDIUM)
        {
            saturation /= 2;
        }
        else if (Global.Difficulty == GameDifficulty.HARD)
        {
            saturation /= 4;
        }
        blobsLeft = lastBlobCount = (int)(BLOB_COUNT * saturation);
        var shapeNode = GetNode<CollisionShape2D>("CollisionShape2D");
        var shape = shapeNode.Shape as RectangleShape2D;
        extentX = shape.Extents.x * 2f;
        extentY = shape.Extents.y * 2f;
        var particles = GetNode<CPUParticles2D>("CPUParticles2D");
        particles.EmissionRectExtents = shape.Extents;

        if (blobsLeft <= 0)
        {
            particles.QueueFree();
            if (HasNode("CollisionPolygon2D"))
            {
                var collisionPolygon = GetNode<CollisionPolygon2D>("CollisionPolygon2D");
                var oldAutoPolygon = shapeNode.GetNode<AutoPolygon>("AutoPolygon");
                var newAutoPolygon = new AutoPolygon();
                newAutoPolygon.Color = oldAutoPolygon.Color;
                newAutoPolygon.Material = oldAutoPolygon.Material;
                collisionPolygon.AddChild(newAutoPolygon);
                shapeNode.QueueFree();

                newAutoPolygon.Polygon = collisionPolygon.Polygon;
                GD.Print(GetPath(), ": ", collisionPolygon.Polygon.Length);
            }
        }
        else
        {
            particleEmitters.Add(particles);
            particles.Amount = PARTICLES_PER_EMITTER;
            while (particleEmitters.Count < PARTICLE_RESOLUTION)
            {
                CPUParticles2D newParticles = (CPUParticles2D)particles.Duplicate();
                AddChild(newParticles);
                particleEmitters.Add(newParticles);
            }
        }
    }

    public override void _Process(float delta)
    {
        if (blobsLeft != lastBlobCount)
        {
            UpdateParticleEmitters();
            lastBlobCount = blobsLeft;
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        blobTakeCooldown = Mathf.Max(0f, blobTakeCooldown - delta);
    }

    public Vector2 GetRandomPosition()
    {
        return GlobalPosition + new Vector2((GD.Randf() - .5f) * extentX, (GD.Randf() - .5f) * extentY);
    }

    public void ReceiveBlob() => ++blobsLeft;

    public bool TryToTakeBlob()
    {
        if (blobsLeft <= 0 || blobTakeCooldown > 0f)
        {
            return false;
        }
        blobTakeCooldown = BLOB_TAKE_COOLDOWN;
        --blobsLeft;
        UpdateParticleEmitters();
        return true;
    }

    private void UpdateParticleEmitters()
    {
        int index = (blobsLeft * PARTICLE_RESOLUTION) / BLOB_COUNT;
        float visibility = ((float)(blobsLeft * PARTICLE_RESOLUTION) / BLOB_COUNT) % 1f;
        for (int previousIndex = index - 1; previousIndex >= 0; previousIndex--)
        {
            particleEmitters[previousIndex].Modulate = new Color(particleEmitters[previousIndex].Modulate, 1f);
            particleEmitters[previousIndex].Visible = particleEmitters[previousIndex].Emitting = true;
        }
        particleEmitters[index].Modulate = new Color(particleEmitters[index].Modulate, Mathf.Min(visibility + .25f, 1f));
        particleEmitters[index].Visible = particleEmitters[index].Emitting = true;
        for (int laterIndex = index + 1; laterIndex < PARTICLE_RESOLUTION; laterIndex++)
        {
            particleEmitters[laterIndex].Emitting = particleEmitters[laterIndex].Visible = false;
        }
    }
}
