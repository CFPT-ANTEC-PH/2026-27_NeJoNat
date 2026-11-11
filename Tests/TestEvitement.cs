using Godot;
using RobotMaker.Scripts;

namespace RobotMaker.Tests;

// Robot à capteurs avant lancé droit sur un mur : il doit l'éviter sans le toucher, en tournant
// progressivement (pas de braquage brutal). Avant le lissage : rotation max 1,76 rad/s, à-coup 0,075.
// Lancer : godot --headless --fixed-fps 60 res://Tests/TestEvitement.tscn
public partial class TestEvitement : Node3D
{
    private const float FaceMur = 10f;   // z de la face du mur

    private Robot _robot;
    private float _temps, _rotationMax, _acoupMax, _precedente, _plusPres = float.MaxValue;

    public override void _Ready()
    {
        var sol = new StaticBody3D { CollisionLayer = 9 };
        sol.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(100, 1, 100) }, Position = Vector3.Down * 0.5f });
        AddChild(sol);

        var mur = new StaticBody3D { Position = new Vector3(0, 1, FaceMur + 0.5f) };
        mur.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(20, 2, 1) } });
        AddChild(mur);

        _robot = GD.Load<PackedScene>("res://Scenes/robot.tscn").Instantiate<Robot>();
        _robot.Capteurs = Robot.TypeCapteurs.Avant;
        _robot.Position = new Vector3(0, 0.1f, 0);
        AddChild(_robot);
    }

    public override void _PhysicsProcess(double delta)
    {
        _temps += (float)delta;
        float w = _robot.AngularVelocity.Y;
        _rotationMax = Mathf.Max(_rotationMax, Mathf.Abs(w));
        _acoupMax = Mathf.Max(_acoupMax, Mathf.Abs(w - _precedente));
        _precedente = w;
        // Coins avant du pare-chocs et bout du robot
        foreach (Vector3 p in new[] { new Vector3(-0.9f, 0.6f, 2.8f), new Vector3(0.9f, 0.6f, 2.8f), new Vector3(0, 0.6f, 3.1f) })
            _plusPres = Mathf.Min(_plusPres, FaceMur - _robot.ToGlobal(p).Z);

        if (_temps < 10f)
            return;

        float cap = Mathf.Abs(Mathf.RadToDeg(_robot.GlobalRotation.Y));
        bool ok = _plusPres > 0.5f && cap > 45f && _rotationMax < 1.5f && _acoupMax < 0.065f;
        GD.Print($"TestEvitement {(ok ? "OK" : "ECHEC")} : au plus près {_plusPres * 10f:0} cm du mur, tourné de {cap:0}°, "
                 + $"rotation max {_rotationMax:0.00} rad/s, plus gros à-coup {_acoupMax:0.000}");
        GetTree().Quit(ok ? 0 : 1);
    }
}
