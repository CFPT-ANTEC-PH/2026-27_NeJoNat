using Godot;
using RobotMaker.InteractiveMap;
using RobotMaker.Scripts;

namespace RobotMaker.Tests;

// Un robot à capteurs bas, posé sur une ligne droite puis en virage, doit la suivre jusqu'au bout.
// Lancer : godot --headless res://Tests/TestSuiviLigne.tscn
public partial class TestSuiviLigne : Node3D
{
    private static readonly Vector3[] Trace = { new(0, 0, -1), new(0, 0, 10), new(6, 0, 20) };

    private Robot _robot;
    private float _temps, _ecartMax;

    public override void _Ready()
    {
        _robot = GetNode<Robot>("Robot");
        var ligne = GetNode<LineDrawer>("Ligne");
        for (int i = 1; i < Trace.Length; i++)
            for (float t = 0f; t < 1f; t += 0.05f)   // petits segments, comme un tracé à la souris
                ligne.AjouterSegment(Trace[i - 1].Lerp(Trace[i], t), Trace[i - 1].Lerp(Trace[i], t + 0.05f));
    }

    // Distance (au sol) entre le support des capteurs bas et la ligne
    private float Ecart()
    {
        Vector3 p = _robot.SupportBas.GlobalPosition;
        p.Y = 0f;
        float d = float.PositiveInfinity;
        for (int i = 1; i < Trace.Length; i++)
            d = Mathf.Min(d, p.DistanceTo(Geometry3D.GetClosestPointToSegment(p, Trace[i - 1], Trace[i])));
        return d;
    }

    public override void _PhysicsProcess(double delta)
    {
        _temps += (float)delta;
        if (_temps < 1f)
            return;   // laisse le robot se poser
        _ecartMax = Mathf.Max(_ecartMax, Ecart());

        if (_temps < 12f && _robot.SupportBas.GlobalPosition.Z < 18f)
            return;

        // Doit avoir franchi le virage sans jamais s'écarter de plus de ~la demi-largeur du support
        float z = _robot.SupportBas.GlobalPosition.Z;
        bool ok = z >= 18f && _ecartMax < 0.8f;
        GD.Print($"TestSuiviLigne {(ok ? "OK" : "ECHEC")} : capteurs à z = {z:0.0} (attendu ≥ 18) en {_temps:0.0} s, "
                 + $"écart max à la ligne {_ecartMax:0.00} (limite 0.80)");
        GetTree().Quit(ok ? 0 : 1);
    }
}
