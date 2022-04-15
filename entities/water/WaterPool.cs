using System.Collections.Generic;
using Godot;

public class WaterPool : Area2D
{
    private const int PARTICLE_RESOLUTION = 5;
    private const int TOTAL_PARTICLES = 100;
    private const int PARTICLES_PER_EMITTER = TOTAL_PARTICLES / PARTICLE_RESOLUTION;
    private const int BLOB_COUNT = 100;
    private const float BLOB_TAKE_COOLDOWN = .05f;
    private float blobTakeCooldown = 0f;

    private float extentX = 0f;
    private float extentY = 0f;

    private int lastBlobCount = BLOB_COUNT;
    private int blobsLeft = BLOB_COUNT;
    private List<CPUParticles2D> particleEmitters = new List<CPUParticles2D>(PARTICLE_RESOLUTION);

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
        var shape = GetNode<CollisionShape2D>("CollisionShape2D").Shape as RectangleShape2D;
        extentX = shape.Extents.x * 2f;
        extentY = shape.Extents.y * 2f;
        var particles = GetNode<CPUParticles2D>("CPUParticles2D");
        particles.EmissionRectExtents = shape.Extents;

        particleEmitters.Add(particles);
        particles.Amount = PARTICLES_PER_EMITTER;
        while (particleEmitters.Count < PARTICLE_RESOLUTION)
        {
            CPUParticles2D newParticles = (CPUParticles2D)particles.Duplicate();
            AddChild(newParticles);
            particleEmitters.Add(newParticles);
        }
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
