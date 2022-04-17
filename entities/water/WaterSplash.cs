using Godot;

public class WaterSplash : CPUParticles2D
{
    private const float MIN_VELOCITY = 30f;
    private const float Max_VELOCITY = 80f;

    private const int MIN_AMOUNT = 10;
    private const int MAX_AMOUNT = 100;

    public float Intensity
    {
        set
        {
            InitialVelocity = Mathf.Lerp(MIN_VELOCITY, Max_VELOCITY, value);
            Amount = (int)Mathf.Lerp(MIN_AMOUNT, MAX_AMOUNT, value);
            ScaleAmount = Mathf.Sqrt(value);
        }
    }

    public override void _Ready()
    {
        Emitting = true;
    }

    public void _on_Timer_timeout()
    {
        QueueFree();
    }
}
