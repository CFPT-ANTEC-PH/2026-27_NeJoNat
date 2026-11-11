using Godot;
using System.Collections.Generic;

namespace RobotMaker.Scripts;

// Capteur de ligne analogique (comme un capteur infrarouge à réflexion) : il voit une petite tache du sol
// et mesure quelle part en est noire, de 0 (sol) à 1 (ligne). À moitié sur le bord de la ligne, il lit 0,5.
// La ligne de l'InteractiveMap est sur le calque de collision 2, le sol non.
public partial class CapteurRoute : CsgBox3D
{
    [Export] public float Portee = 1f;                           // distance max jusqu'au sol en unités (1 u = 10 cm)
    [Export(PropertyHint.Layers3DPhysics)] public uint Masque = 2;
    [Export] public float Tache = 0.15f;                         // u, diamètre de la zone vue au sol (1,5 cm : largeur du capteur)
    [Export(PropertyHint.Range, "0,0.1,0.005")] public float Bruit = 0.01f;   // écart-type de la mesure (0 = capteur parfait) ; 1 % : lecture moyennée sur plusieurs conversions, comme sur un vrai robot

    [ExportGroup("Couleurs")]
    [Export] public Color CouleurSol = new(1f, 1f, 1f);
    [Export] public Color CouleurLigne = new(0.1f, 0.1f, 0.1f);

    // Résultats lus par le robot
    public float Valeur { get; private set; }     // 0 = sol, 1 = ligne
    public bool SurLigne => Valeur > 0.5f;

    // Points de la tache, en grille dans un disque : 21 rayons par mesure
    private static readonly List<Vector2> Points = new();
    private StandardMaterial3D _materiau;

    static CapteurRoute()
    {
        for (int x = -2; x <= 2; x++)
            for (int z = -2; z <= 2; z++)
                if (x * x + z * z <= 5)
                    Points.Add(new Vector2(x, z) / 4f);   // rayon 0,5 : multiplié par Tache
    }

    public override void _Ready()
    {
        // Un matériau par instance, sinon les 5 capteurs changeraient de couleur ensemble
        _materiau = new StandardMaterial3D { AlbedoColor = CouleurSol };
        Material = _materiau;
    }

    public override void _PhysicsProcess(double delta)
    {
        var espace = GetWorld3D().DirectSpaceState;
        Vector3 bas = -GlobalBasis.Y.Normalized() * Portee;
        Vector3 x = GlobalBasis.X.Normalized() * Tache, z = GlobalBasis.Z.Normalized() * Tache;

        int noirs = 0;
        foreach (Vector2 p in Points)
        {
            Vector3 depart = GlobalPosition + x * p.X + z * p.Y;
            if (espace.IntersectRay(PhysicsRayQueryParameters3D.Create(depart, depart + bas, Masque)).Count > 0)
                noirs++;
        }
        Valeur = Mathf.Clamp((float)noirs / Points.Count + (float)GD.Randfn(0, Bruit), 0f, 1f);

        _materiau.AlbedoColor = CouleurSol.Lerp(CouleurLigne, Valeur);
    }
}
