using Godot;

namespace RobotMaker.InteractiveMap;

// Ligne noire dessinée au sol à la souris (clic gauche maintenu).
// Chaque segment a une collision sur le calque 2 (réglé dans la scène) :
// un capteur de ligne n'a qu'à faire un raycast vers le bas avec le masque 2.
public partial class LineDrawer : StaticBody3D
{
    [Export] public float Largeur = 0.4f;
    [Export] public float Epaisseur = 0.01f;
    [Export] public float Pas = 0.2f;   // distance mini entre deux points du tracé

    public bool Actif;

    private Vector3? _dernier;
    private readonly StandardMaterial3D _noir = new() { AlbedoColor = Colors.Black };

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Actif)
            return;

        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            _dernier = Pick.SousLaSouris(this);
        }
        else if (e is InputEventMouseMotion souris && souris.ButtonMask.HasFlag(MouseButtonMask.Left)
                 && _dernier is Vector3 a && Pick.SousLaSouris(this) is Vector3 b && a.DistanceTo(b) >= Pas)
        {
            AjouterSegment(a, b);
            _dernier = b;
        }
    }

    // ponytail: un nœud par segment, fusionner en un seul mesh si de très longs tracés font ramer
    private void AjouterSegment(Vector3 a, Vector3 b)
    {
        Vector3 direction = b - a;
        direction.Y = 0f;
        if (direction.IsZeroApprox())
            return;

        // + Largeur pour que les segments se chevauchent et ne laissent pas de trous dans les virages
        var taille = new Vector3(Largeur, Epaisseur, direction.Length() + Largeur);
        var transform = new Transform3D(Basis.LookingAt(direction, Vector3.Up), (a + b) / 2f + Vector3.Up * Epaisseur / 2f);

        AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = taille, Material = _noir }, Transform = transform });
        AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = taille }, Transform = transform });
    }

    public void Effacer()
    {
        foreach (Node enfant in GetChildren())
            enfant.QueueFree();
    }
}
