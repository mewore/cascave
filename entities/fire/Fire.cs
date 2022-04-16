using Godot;
using System;
using System.Collections.Generic;

public class Fire : Node2D
{
    [Export(PropertyHint.Range, "1, 30")]
    private int hitPoints = 5;
    private int currentHitPoints;

    private double log10HitPoints;
    private float originalVolume;

    private CPUParticles2D[] fireParticleEmitters;
    private CPUParticles2D[] smokeParticleEmitters;
    private AudioStreamPlayer2D fireSound;

    public override void _Ready()
    {
        currentHitPoints = hitPoints;
        fireParticleEmitters = ReplicateParticles(GetNode<CPUParticles2D>("Fire"));
        smokeParticleEmitters = ReplicateParticles(GetNode<CPUParticles2D>("Smoke"));
        fireSound = GetNode<AudioStreamPlayer2D>("FireSound");
        originalVolume = fireSound.VolumeDb;
        log10HitPoints = Math.Log10(hitPoints);
    }

    private CPUParticles2D[] ReplicateParticles(CPUParticles2D original)
    {
        int totalParticleCount = original.Amount;
        List<CPUParticles2D> emitters = new List<CPUParticles2D>(hitPoints);
        original.Amount = totalParticleCount / hitPoints;
        emitters.Add(original);
        for (int i = 1; i < hitPoints; i++)
        {
            var replicated = original.Duplicate((int)Godot.Node.DuplicateFlags.Groups) as CPUParticles2D;
            AddChild(replicated);
            emitters.Add(replicated);
        }
        return emitters.ToArray();
    }

    public bool TakeDamage()
    {
        if (currentHitPoints <= 0)
        {
            return false;
        }
        --currentHitPoints;
        fireParticleEmitters[currentHitPoints].Emitting = smokeParticleEmitters[currentHitPoints].Emitting = false;
        if (currentHitPoints <= 0)
        {
            fireSound.Stop();
        }
        else
        {
            fireSound.VolumeDb = (float)(originalVolume + Math.Log10(currentHitPoints) - log10HitPoints);
        }
        return true;
    }
}
