using Godot;

namespace RobotMaker.Scripts;

public partial class CapteurAvant : CsgBox3D
{
    [Export] public float Portee = 3f;                           // distance de détection en mètres
    [Export] public Vector3 DirectionLocale = Vector3.Back;      // +Z local du capteur
    [Export(PropertyHint.Layers3DPhysics)] public uint Masque = 1;

    [ExportGroup("Couleurs")]
    [Export] public Color CouleurLibre = new(0.1f, 0.2f, 1f);
    [Export] public Color CouleurDetecte = new(1f, 0.1f, 0.1f);

    [ExportGroup("Debug")]
    [Export] public bool AfficherRayon = true;

    // Résultats lus par le robot
    public bool Detecte { get; private set; }
    public float Distance { get; private set; } = float.PositiveInfinity;
    public Vector3 Point { get; private set; }
    public Node Objet { get; private set; }

    private CollisionObject3D _robot;
    private StandardMaterial3D _materiau;

    // Ligne de debug
    private MeshInstance3D _ligne;
    private ImmediateMesh _meshLigne;
    private StandardMaterial3D _materiauLigne;

    public override void _Ready()
    {
        _robot = TrouverRobot();

        // Un matériau par instance, sinon les 3 capteurs changeraient de couleur ensemble
        _materiau = new StandardMaterial3D { AlbedoColor = CouleurLibre };
        Material = _materiau;

        CreerLigne();
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 depart = GlobalPosition;
        Vector3 direction = (GlobalBasis * DirectionLocale).Normalized();
        Vector3 arrivee = depart + direction * Portee;

        var requete = PhysicsRayQueryParameters3D.Create(depart, arrivee, Masque);
        if (_robot != null)
            requete.Exclude = new Godot.Collections.Array<Rid> { _robot.GetRid() };

        var resultat = GetWorld3D().DirectSpaceState.IntersectRay(requete);

        if (resultat.Count > 0)
        {
            Point = (Vector3)resultat["position"];
            Distance = depart.DistanceTo(Point);
            Objet = (Node)resultat["collider"];
            Detecte = true;
        }
        else
        {
            Distance = float.PositiveInfinity;
            Objet = null;
            Detecte = false;
        }

        _materiau.AlbedoColor = Detecte ? CouleurDetecte : CouleurLibre;

        DessinerLigne(depart, Detecte ? Point : arrivee);
    }

    private void CreerLigne()
    {
        _meshLigne = new ImmediateMesh();

        _materiauLigne = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,  // couleur pure, pas d'ombre
            NoDepthTest = true,                                     // visible même à travers le robot
            VertexColorUseAsAlbedo = true
        };

        _ligne = new MeshInstance3D
        {
            Mesh = _meshLigne,
            TopLevel = true,  // ignore le scale du cube
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };

        AddChild(_ligne);
        _ligne.GlobalTransform = Transform3D.Identity;  // on dessine en coordonnées monde
    }

    private void DessinerLigne(Vector3 depart, Vector3 fin)
    {
        _meshLigne.ClearSurfaces();
        _ligne.Visible = AfficherRayon;

        if (!AfficherRayon)
            return;

        Color couleur = Detecte ? CouleurDetecte : CouleurLibre;

        _meshLigne.SurfaceBegin(Mesh.PrimitiveType.Lines, _materiauLigne);
        _meshLigne.SurfaceSetColor(couleur);
        _meshLigne.SurfaceAddVertex(depart);
        _meshLigne.SurfaceSetColor(couleur);
        _meshLigne.SurfaceAddVertex(fin);
        _meshLigne.SurfaceEnd();
    }

    // Remonte l'arbre pour trouver le robot (pour ne pas se détecter soi-même)
    private CollisionObject3D TrouverRobot()
    {
        Node n = GetParent();
        while (n != null)
        {
            if (n is CollisionObject3D corps)
                return corps;
            n = n.GetParent();
        }
        return null;
    }
}