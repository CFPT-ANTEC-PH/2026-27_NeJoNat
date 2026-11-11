using Godot;

namespace RobotMaker.Tests;

// Une balle lancée à 30 cm/s sur la table doit s'arrêter comme une vraie (résistance au roulement),
// pas rouler sans fin. Lancer : godot --headless --fixed-fps 60 res://Tests/TestRoulement.tscn
public partial class TestRoulement : Node3D
{
    private RigidBody3D _balle;
    private float _temps, _depart;

    public override void _Ready()
    {
        var sol = new StaticBody3D { CollisionLayer = 9 };
        sol.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(100, 1, 100) }, Position = Vector3.Down * 0.5f });
        AddChild(sol);

        _balle = GD.Load<PackedScene>("res://InteractiveMap/Objects/Sphere.tscn").Instantiate<RigidBody3D>();
        _balle.Position = new Vector3(0, 0.5f, 0);
        AddChild(_balle);
    }

    public override void _PhysicsProcess(double delta)
    {
        _temps += (float)delta;
        if (_temps < 0.5f)
            return;   // se pose
        if (_depart == 0f)
        {
            // Lancée en roulant déjà (sans glisser) : v = ω × r
            _balle.LinearVelocity = new Vector3(0, 0, 3);
            _balle.AngularVelocity = new Vector3(3 / 0.5f, 0, 0);
            _depart = _temps;
            return;
        }

        float duree = _temps - _depart;
        bool arretee = _balle.LinearVelocity.Length() < 0.05f;
        if (!arretee && duree < 10f)
            return;

        // Balle en caoutchouc creuse, C = 0,02 : environ 4 s et 45 cm
        float distance = _balle.GlobalPosition.Z;
        bool ok = arretee && duree is > 2f and < 6f && distance is > 2f and < 8f;
        GD.Print($"TestRoulement {(ok ? "OK" : "ECHEC")} : {(arretee ? "arrêtée" : "roule encore")} après {duree:0.0} s et {distance * 10f:0} cm");
        GetTree().Quit(ok ? 0 : 1);
    }
}
