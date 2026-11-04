using Godot;

namespace RobotMaker.Scripts;

// Capteur de ligne : regarde vers le bas et dit s'il est au-dessus de la ligne noire.
// La ligne de l'InteractiveMap est sur le calque de collision 2, le sol non.
public partial class CapteurRoute : CsgBox3D
{
    [Export] public float Portee = 1f;                           // distance max jusqu'au sol en mètres
    [Export(PropertyHint.Layers3DPhysics)] public uint Masque = 2;

    [ExportGroup("Couleurs")]
    [Export] public Color CouleurSol = new(1f, 1f, 1f);
    [Export] public Color CouleurLigne = new(0.1f, 0.1f, 0.1f);

    // Résultat lu par le robot
    public bool SurLigne { get; private set; }

    private StandardMaterial3D _materiau;

    public override void _Ready()
    {
        // Un matériau par instance, sinon les 5 capteurs changeraient de couleur ensemble
        _materiau = new StandardMaterial3D { AlbedoColor = CouleurSol };
        Material = _materiau;
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 depart = GlobalPosition;
        Vector3 arrivee = depart - GlobalBasis.Y.Normalized() * Portee;

        var requete = PhysicsRayQueryParameters3D.Create(depart, arrivee, Masque);
        SurLigne = GetWorld3D().DirectSpaceState.IntersectRay(requete).Count > 0;

        _materiau.AlbedoColor = SurLigne ? CouleurLigne : CouleurSol;
    }
}
