using Godot;

public class MouseCamera : Camera2D
{
    [Export]
    private NodePath player = null;
    private Node2D playerNode;

    [Export(PropertyHint.Range, "0, 1")]
    private float mouseFollow = .5f;

    public override void _Ready()
    {
        if (player != null)
        {
            playerNode = GetNode<Node2D>(player);
            Position = playerNode.GlobalPosition;
        }
    }

    public override void _Process(float delta)
    {
        if (playerNode != null)
        {
            Position = playerNode.GlobalPosition * (1f - mouseFollow * .5f) + GetGlobalMousePosition() * mouseFollow * .5f;
        }
    }
}
