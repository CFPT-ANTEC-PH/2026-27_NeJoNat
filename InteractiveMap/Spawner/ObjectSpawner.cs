using Godot;
using RobotMaker.Scripts;

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
        // Placé avant d'entrer dans l'arbre : le robot retient cette position pour « Replacer »
        float hauteur = objet is Robot ? 0.1f : HauteurLacher;
        objet.Position = ToLocal(point + Vector3.Up * hauteur);
        AddChild(objet);
    }

    public void Vider()
    {
        foreach (Node enfant in GetChildren())
            enfant.QueueFree();
    }
}
