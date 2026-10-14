using Godot;

namespace RobotMaker.InteractiveMap;

// Pose l'objet sélectionné dans le menu là où on clique (il tombe de HauteurLacher)
public partial class ObjectSpawner : Node3D
{
    [Export] public float HauteurLacher = 1f;

    public PackedScene Selection;

    public override void _UnhandledInput(InputEvent e)
    {
        if (Selection == null || e is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
            return;

        if (Pick.SousLaSouris(this) is not Vector3 point)
            return;

        var objet = Selection.Instantiate<Node3D>();
        AddChild(objet);
        objet.GlobalPosition = point + Vector3.Up * HauteurLacher;
    }

    public void Vider()
    {
        foreach (Node enfant in GetChildren())
            enfant.QueueFree();
    }
}
