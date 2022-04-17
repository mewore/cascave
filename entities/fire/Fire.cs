using Godot;
using System;
using System.Collections.Generic;

public class Fire : Node2D
{
    public const string LIGHT_FLICKERS_SETTING = "application/game/fire_light_flickers";

    [Export(PropertyHint.Range, "1, 30")]
    private int hitPoints = 5;
    private int currentHitPoints;

    public bool IsExtinguished => currentHitPoints == 0;

    private double log10HitPoints;
    private float originalVolume;

    private CPUParticles2D[] fireParticleEmitters;
    private CPUParticles2D[] smokeParticleEmitters;
    private AudioStreamPlayer2D fireSound;

    private float lightEnergyVariation = .5f;
    private float lightEnergyChangeSpeed = 5f;
    private float originalLightEnergy;
    private Light2D light;
    private Sprite lightSprite;
    private bool ShouldInduceSeizures => (bool)ProjectSettings.GetSetting(LIGHT_FLICKERS_SETTING);

    public override void _Ready()
    {
        currentHitPoints = hitPoints;
        fireParticleEmitters = ReplicateParticles(GetNode<CPUParticles2D>("Fire"));
        smokeParticleEmitters = ReplicateParticles(GetNode<CPUParticles2D>("Smoke"));
        fireSound = GetNode<AudioStreamPlayer2D>("FireSound");
        originalVolume = fireSound.VolumeDb;
        log10HitPoints = Math.Log10(hitPoints);
        light = GetNode<Light2D>("Light2D");
        originalLightEnergy = light.Energy;
        lightSprite = GetNode<Sprite>("LightSprite");
        ApplyLightingSetting(Global.CurrentLightingSetting);
        Global.SINGLETON.Connect(nameof(Global.NewLightingSetting), this, "ApplyLightingSetting");
    }

    public override void _Process(float delta)
    {
        if (currentHitPoints <= 0)
        {
            return;
        }

        if (ShouldInduceSeizures)
        {
            float averageLightEnergy = originalLightEnergy * currentHitPoints / hitPoints;
            float variation = averageLightEnergy * lightEnergyVariation * .5f;
            light.Energy = Mathf.Clamp(
                light.Energy + ((float)Math.Pow(GD.Randf() * 2 - 1f, 3) * .5f) * averageLightEnergy * lightEnergyChangeSpeed * delta,
                Mathf.Max(0f, averageLightEnergy - variation),
                averageLightEnergy + variation);
            lightSprite.Modulate = new Color(lightSprite.Modulate, light.Energy);
        }
    }

    public void ApplyLightingSetting(LightingSetting setting)
    {
        if (currentHitPoints <= 0)
        {
            return;
        }
        light.Visible = setting == LightingSetting.NO_SHADOWS || setting == LightingSetting.WITH_SHADOWS;
        light.ShadowEnabled = setting == LightingSetting.WITH_SHADOWS;
        lightSprite.Visible = setting == LightingSetting.SPRITES;
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
            light.QueueFree();
            lightSprite.QueueFree();
            fireSound.Stop();
        }
        else
        {
            light.Energy = originalLightEnergy * currentHitPoints / hitPoints;
            lightSprite.Modulate = new Color(lightSprite.Modulate, light.Energy);
            fireSound.VolumeDb = (float)(originalVolume + Math.Log10(currentHitPoints) - log10HitPoints);
        }
        return true;
    }
}
