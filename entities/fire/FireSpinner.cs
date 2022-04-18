using Godot;

[Tool]
public class FireSpinner : Node2D
{
    [Export]
    private float rotationSpeed = Mathf.Tau;

    [Export]
    private Curve rotationSpeedCurve = null;

    [Export]
    private float rotationSpeedCurvePeriod = 1f;

    private float now = 0f;

    public override void _Ready()
    {
        if (Global.Difficulty == GameDifficulty.MEDIUM)
        {
            rotationSpeed *= 1.5f;
        }
        else if (Global.Difficulty == GameDifficulty.HARD)
        {
            rotationSpeed *= 3f;
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        float speed = rotationSpeedCurve == null
            ? rotationSpeed
            : rotationSpeed * (rotationSpeedCurve.InterpolateBaked((now % rotationSpeedCurvePeriod) / rotationSpeedCurvePeriod));
        now += delta;
        Rotation += speed * delta;
    }
}
