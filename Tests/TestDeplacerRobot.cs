using Godot;
using RobotMaker.InteractiveMap;
using RobotMaker.Scripts;

namespace RobotMaker.Tests;

// Glisser-déposer : on saisit le robot 1 m devant son origine, on le porte en (5, 5), on le tourne de 90°
// et on le lâche. Il doit s'y poser, orienté, dégelé, et « Replacer » doit le ramener là.
// Lancer : godot --headless res://Tests/TestDeplacerRobot.tscn
public partial class TestDeplacerRobot : Node3D
{
    private static readonly Vector3 Attendu = new(4f, 0f, 5f);   // (5, 5) + décalage (0, 0, -1) tourné de 90°

    private Robot _robot;
    private DeplaceurRobots _deplaceur;
    private float _temps;
    private int _etape;
    private bool _ok = true;
    private string _detail = "";

    public override void _Ready()
    {
        _robot = GetNode<Robot>("Robot");
        _deplaceur = GetNode<DeplaceurRobots>("DeplaceurRobots");
    }

    private void Verifier(string quoi, bool condition)
    {
        if (!condition)
            (_ok, _detail) = (false, _detail + $" {quoi} (position {_robot.GlobalPosition}, cap {Mathf.RadToDeg(_robot.GlobalRotation.Y):0}°)");
    }

    private static float APlat(Vector3 a, Vector3 b) => new Vector2(a.X - b.X, a.Z - b.Z).Length();

    public override void _PhysicsProcess(double delta)
    {
        _temps += (float)delta;
        if (_etape == 0 && _temps > 0.5f)
        {
            Vector3 saisi = _robot.GlobalPosition + new Vector3(0f, 0f, 1f);
            saisi.Y = 0f;
            _deplaceur.Prendre(_robot, saisi);
            _deplaceur.Bouger(new Vector3(5f, 0f, 5f));
            _deplaceur.Tourner(90f);
            Verifier("porté en l'air", _robot.Freeze && _robot.GlobalPosition.Y > 0.4f);
            _deplaceur.Lacher();
            Verifier("lâché", !_deplaceur.Porte && !_robot.Freeze);
            _etape = 1;
        }
        else if (_etape == 1 && _temps > 1.5f)
        {
            Verifier("posé au bon endroit", APlat(_robot.GlobalPosition, Attendu) < 0.1f && _robot.GlobalPosition.Y < 0.2f);
            Verifier("tourné de 90°", Mathf.Abs(Mathf.AngleDifference(_robot.GlobalRotation.Y, Mathf.Pi / 2f)) < 0.05f);
            _robot.GlobalPosition = new Vector3(-6f, 0.1f, -6f);
            _robot.Replacer();
            Verifier("Replacer ramène au dépôt", APlat(_robot.GlobalPosition, Attendu) < 0.1f);

            GD.Print(_ok ? "TestDeplacerRobot OK" : "TestDeplacerRobot ECHEC :" + _detail);
            GetTree().Quit(_ok ? 0 : 1);
        }
    }
}
