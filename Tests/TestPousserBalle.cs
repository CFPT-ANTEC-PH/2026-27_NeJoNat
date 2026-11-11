using Godot;
using RobotMaker.Scripts;

namespace RobotMaker.Tests;

// Le robot avance tout droit dans une balle : il doit la pousser sans se bloquer ni se soulever
// (pare-chocs trop haut = il appuie la balle contre le sol et monte dessus).
// Lancer : godot --headless --fixed-fps 60 res://Tests/TestPousserBalle.tscn
public partial class TestPousserBalle : Node3D
{
    private Robot _robot;
    private RigidBody3D _balle;
    private float _temps, _penteMax;

    public override void _Ready()
    {
        var sol = new StaticBody3D { CollisionLayer = 9 };
        sol.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(100, 1, 100) }, Position = Vector3.Down * 0.5f });
        AddChild(sol);

        _robot = GD.Load<PackedScene>("res://Scenes/robot.tscn").Instantiate<Robot>();
        _robot.Capteurs = Robot.TypeCapteurs.Bas;   // sans anti-collision : il avance tout droit, ligne pas trouvée
        _robot.Position = new Vector3(0, 0.1f, 0);
        AddChild(_robot);

        _balle = GD.Load<PackedScene>("res://InteractiveMap/Objects/Sphere.tscn").Instantiate<RigidBody3D>();
        _balle.Position = new Vector3(0, 0.6f, 6);
        AddChild(_balle);
    }

    public override void _PhysicsProcess(double delta)
    {
        _temps += (float)delta;
        _penteMax = Mathf.Max(_penteMax, Mathf.RadToDeg(Mathf.Acos(Mathf.Clamp(_robot.GlobalBasis.Y.Normalized().Y, -1f, 1f))));
        if (_temps < 8f)
            return;

        // Sans balle, il ferait ~7,6 u en 8 s ; bloqué, il restait vers 4,6 u avec une pente de 7,7°
        float z = _robot.GlobalPosition.Z;
        bool ok = z > 7f && _balle.GlobalPosition.Z > z && _penteMax < 3f;
        GD.Print($"TestPousserBalle {(ok ? "OK" : "ECHEC")} : robot à {z:0.00} u, balle à {_balle.GlobalPosition.Z:0.00} u, pente max {_penteMax:0.0}°");
        GetTree().Quit(ok ? 0 : 1);
    }
}
