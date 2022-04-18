using Godot;

[Tool]
public class AutoPolygon : Polygon2D
{
    public override async void _Ready()
    {
        Node parent = GetParent<Node>();
        await ToSignal(parent, "ready");
        if (parent is CollisionPolygon2D)
        {
            Polygon = (parent as CollisionPolygon2D).Polygon;
        }
        else if (parent is CollisionShape2D)
        {
            Polygon = PolygonFromShape(parent as CollisionShape2D);
        }
    }

    public override void _Process(float delta)
    {
        if (Engine.EditorHint)
        {
            Node parent = GetParent<Node>();
            if (parent is CollisionPolygon2D)
            {
                Polygon = (parent as CollisionPolygon2D).Polygon;
            }
            else if (parent is CollisionShape2D)
            {
                Polygon = PolygonFromShape(parent as CollisionShape2D);
            }
        }
    }

    private static Vector2[] PolygonFromShape(CollisionShape2D shapeNode)
    {
        var shape = shapeNode.Shape;
        if (shape is RectangleShape2D)
        {
            var rectangle = shape as RectangleShape2D;
            var bottomLeft = new Vector2(-rectangle.Extents.x, rectangle.Extents.y);
            return new Vector2[] { -rectangle.Extents, -bottomLeft, rectangle.Extents, bottomLeft };
        }
        return new Vector2[0];
    }
}
