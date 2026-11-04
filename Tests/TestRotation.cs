using Godot;
using RobotMaker.Scripts;

namespace RobotMaker.Tests;

// Le robot tourne sur place : il doit pivoter autour du milieu de ses roues motrices,
// pas autour de son centre de masse. Lancer : godot --headless res://Tests/TestRotation.tscn
// Passer GripLateral à 0 sur le robot de la scène montre l'ancien comportement (le test échoue).
public partial class TestRotation : Node3D
{
    private Robot _robot;
    private Vector3 _depart;
    private float _temps, _tour, _ecartMax;

    public override void _Ready() => _robot = GetNode<Robot>("Robot");

    private Vector3 Essieu() => (_robot.LeftWheel.GlobalPosition + _robot.RightWheel.GlobalPosition) / 2f;

    public override void _PhysicsProcess(double delta)
    {
        _temps += (float)delta;
        if (_temps < 1f)
            return;   // laisse le robot se poser

        if (!Input.IsActionPressed("left"))
        {
            _depart = Essieu();
            Input.ActionPress("left");
        }

        _tour += _robot.AngularVelocity.Y * (float)delta;
        Vector3 d = Essieu() - _depart;
        d.Y = 0f;
        _ecartMax = Mathf.Max(_ecartMax, d.Length());

        if (_temps < 4f)
            return;

        Input.ActionRelease("left");
        // Le milieu des roues doit rester à moins d'un quart de la voie de son point de départ
        float voie = _robot.LeftWheel.Position.DistanceTo(_robot.RightWheel.Position);
        bool ok = _ecartMax < 0.25f * voie && Mathf.RadToDeg(_tour) > 90f;
        GD.Print($"TestRotation {(ok ? "OK" : "ECHEC")} : tourné de {Mathf.RadToDeg(_tour):0} deg, "
                 + $"milieu des roues déplacé de {_ecartMax:0.00} m au maximum (limite {0.25f * voie:0.00} m)");
        GetTree().Quit(ok ? 0 : 1);
    }
}
