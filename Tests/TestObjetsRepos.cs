using Godot;
using System.Linq;
using RobotMaker.InteractiveMap;

namespace RobotMaker.Tests;

// Cube, cylindre et balle posés comme dans la carte : ils doivent se poser et s'endormir, pas trembler sans fin
// (un cylindre qui apparaissait pile au contact de la table oscillait pour toujours).
// Lancer : godot --headless --fixed-fps 60 res://Tests/TestObjetsRepos.tscn
public partial class TestObjetsRepos : Node3D
{
    private RigidBody3D[] _objets;
    private float _temps;

    public override void _Ready()
    {
        var sol = new StaticBody3D { CollisionLayer = 9 };
        sol.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(100, 1, 100) }, Position = Vector3.Down * 0.5f });
        AddChild(sol);

        var spawner = new ObjectSpawner();
        AddChild(spawner);
        _objets = new[] { "Cube", "Cylindre", "Sphere" }.Select((nom, i) =>
        {
            spawner.Selection = GD.Load<PackedScene>($"res://InteractiveMap/Objects/{nom}.tscn");
            return (RigidBody3D)spawner.Poser(new Vector3(i * 4f, 0f, 0f));
        }).ToArray();
    }

    public override void _PhysicsProcess(double delta)
    {
        _temps += (float)delta;
        if (_temps < 4f)
            return;

        bool ok = _objets.All(o => o.Sleeping);
        GD.Print($"TestObjetsRepos {(ok ? "OK" : "ECHEC")} : "
                 + string.Join(", ", _objets.Select(o => $"{o.Name} {(o.Sleeping ? "au repos" : $"bouge ({o.AngularVelocity.Length():0.00} rad/s)")}")));
        GetTree().Quit(ok ? 0 : 1);
    }
}
