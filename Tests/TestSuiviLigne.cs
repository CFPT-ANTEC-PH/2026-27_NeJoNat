using Godot;
using System.Collections.Generic;
using System.Linq;
using RobotMaker.InteractiveMap;
using RobotMaker.Scripts;

namespace RobotMaker.Tests;

// Un robot à capteurs bas suit plusieurs parcours difficiles, l'un après l'autre.
// Pour chacun il doit atteindre le bout sans jamais s'écarter de la ligne.
// Lancer : godot --headless --fixed-fps 60 res://Tests/TestSuiviLigne.tscn   (--fixed-fps : tourne aussi vite que possible)
public partial class TestSuiviLigne : Node3D
{
    private const float EcartLimite = 0.8f;   // au-delà, plus aucun capteur ne peut voir la ligne
    private const float TempsMax = 30f;

    // Chemin : la ligne idéale (écart et arrivée). Traits : ce qui est dessiné (par défaut le chemin).
    private record Parcours(string Nom, Vector3[] Chemin, Vector3[][] Traits = null, Robot.TypeMoteur Moteur = Robot.TypeMoteur.Normal);

    private static Vector3 P(float x, float z) => new(x, 0f, z);

    private static readonly Vector3[] Droite = { P(0, -1), P(0, 20) };
    private static readonly Vector3[] AngleDroit = { P(0, -1), P(0, 8), P(10, 8) };
    private static readonly Vector3[] Zigzag = { P(0, -1), P(0, 4), P(3, 7), P(3, 10), P(-3, 16), P(-3, 19), P(0, 22) };

    private static readonly Parcours[] Liste =
    {
        new("virage doux", new[] { P(0, -1), P(0, 10), P(6, 20) }),
        new("angle droit", AngleDroit),
        new("angle droit, moteur rapide", AngleDroit, Moteur: Robot.TypeMoteur.Rapide),
        new("épingle", new[] { P(0, -1), P(0, 8), P(4, 8), P(4, 0) }),
        new("zigzag", Zigzag),
        new("zigzag, moteur rapide", Zigzag, Moteur: Robot.TypeMoteur.Rapide),
        new("ligne coupée", Droite, new[] { new[] { P(0, -1), P(0, 8) }, new[] { P(0, 9.5f), P(0, 20) } }),
        new("croisement en biais", Droite, new[] { Droite, new[] { P(-4, 5), P(4, 13) } }),
        new("croisement en T", Droite, new[] { Droite, new[] { P(0, 8), P(6, 8) } }),
    };

    private Robot _robot;
    private LineDrawer _ligne;
    private CapteurRoute[] _capteurs;
    private int _index = -1;
    private float _temps, _ecartMax;
    private readonly List<string> _echecs = new();

    public override void _Ready()
    {
        _robot = GetNode<Robot>("Robot");
        _ligne = GetNode<LineDrawer>("Ligne");
        _capteurs = _robot.SupportBas.GetChildren().OfType<CapteurRoute>().ToArray();
        Suivant();
    }

    private Parcours Actuel => Liste[_index];

    private void Suivant()
    {
        _index++;
        if (_index == Liste.Length)
        {
            GD.Print(_echecs.Count == 0 ? "TestSuiviLigne OK" : $"TestSuiviLigne ECHEC : {string.Join(", ", _echecs)}");
            GetTree().Quit(_echecs.Count == 0 ? 0 : 1);
            return;
        }

        _ligne.Effacer();
        foreach (Vector3[] trait in Actuel.Traits ?? new[] { Actuel.Chemin })
            for (int i = 1; i < trait.Length; i++)
                Tracer(trait[i - 1], trait[i]);

        _robot.Set(nameof(Robot.Moteur), (int)Actuel.Moteur);
        _robot.Appliquer();
        _robot.Replacer();
        _temps = _ecartMax = 0f;
    }

    // Petits segments, comme un tracé à la souris
    private void Tracer(Vector3 a, Vector3 b)
    {
        int n = Mathf.CeilToInt(a.DistanceTo(b) / 0.5f);
        for (int i = 0; i < n; i++)
            _ligne.AjouterSegment(a.Lerp(b, (float)i / n), a.Lerp(b, (float)(i + 1) / n));
    }

    // Milieu de la barre de capteurs, au sol
    private Vector3 Capteurs()
    {
        Vector3 p = _capteurs.Aggregate(Vector3.Zero, (s, c) => s + c.GlobalPosition) / _capteurs.Length;
        p.Y = 0f;
        return p;
    }

    private static float Ecart(Vector3 p, Vector3[] chemin)
    {
        float d = float.PositiveInfinity;
        for (int i = 1; i < chemin.Length; i++)
            d = Mathf.Min(d, p.DistanceTo(Geometry3D.GetClosestPointToSegment(p, chemin[i - 1], chemin[i])));
        return d;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_index >= Liste.Length)
            return;
        _temps += (float)delta;
        if (_temps < 0.5f)
            return;   // laisse le robot se poser

        Vector3 p = Capteurs();
        _ecartMax = Mathf.Max(_ecartMax, Ecart(p, Actuel.Chemin));
        bool arrive = p.DistanceTo(Actuel.Chemin[^1]) < 1f;
        if (!arrive && _temps < TempsMax && _ecartMax < EcartLimite)
            return;

        bool ok = arrive && _ecartMax < EcartLimite;
        GD.Print($"  {(ok ? "ok    " : "ECHEC ")} {Actuel.Nom} : {_temps:0.0} s, écart max {_ecartMax:0.00}"
                 + (arrive ? "" : _ecartMax >= EcartLimite ? " (sorti de la ligne)" : " (bout non atteint)"));
        if (!ok)
            _echecs.Add(Actuel.Nom);
        Suivant();
    }
}
