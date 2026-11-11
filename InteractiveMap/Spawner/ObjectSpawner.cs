using Godot;
using System.Collections.Generic;
using RobotMaker.Scripts;

namespace RobotMaker.InteractiveMap;

// Pose l'objet sélectionné dans le menu là où on clique (il tombe de HauteurLacher)
public partial class ObjectSpawner : Node3D
{
    [Export] public float HauteurLacher = 1f;

    public PackedScene Selection;
    public Dictionary<string, Variant> ConfigRobot;   // réglages des robots posés (null = ceux du constructeur)

    public override void _UnhandledInput(InputEvent e)
    {
        if (Selection == null || e is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
            return;

        if (Pick.SousLaSouris(this) is Vector3 point)
            Poser(point);
    }

    public Node3D Poser(Vector3 point)
    {
        var objet = Selection.Instantiate<Node3D>();
        // Placé avant d'entrer dans l'arbre : le robot retient cette position pour « Replacer »
        float hauteur = HauteurLacher;
        if (objet is Robot robot)
        {
            robot.Config = ConfigRobot;
            robot.AfficherNom = true;
            hauteur = 0.1f;
        }
        objet.Position = ToLocal(point + Vector3.Up * hauteur);
        AddChild(objet);
        // Posé dans un autre robot, un objet ou un mur : on n'en veut pas (comme au glisser-déposer)
        if (objet is Robot r && r.Chevauche(r.GlobalTransform))
        {
            objet.QueueFree();
            return null;
        }
        return objet;
    }

    public void Vider()
    {
        foreach (Node enfant in GetChildren())
            enfant.QueueFree();
    }
}
