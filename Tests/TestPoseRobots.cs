using Godot;
using RobotMaker.InteractiveMap;
using RobotMaker.Scripts;

namespace RobotMaker.Tests;

// Deux robots posés avec des sauvegardes différentes gardent chacun leurs réglages,
// sans toucher à ceux du constructeur (Robot.Choix). Lancer : godot --headless res://Tests/TestPoseRobots.tscn
public partial class TestPoseRobots : Node3D
{
    public override void _Ready()
    {
        Robot.Choix = new() { [nameof(Robot.NomRobot)] = "Constructeur", [nameof(Robot.Moteur)] = (int)Robot.TypeMoteur.Lent };
        var spawner = GetNode<ObjectSpawner>("Objets");
        spawner.Selection = GD.Load<PackedScene>("res://Scenes/robot.tscn");

        spawner.ConfigRobot = new()
        {
            [nameof(Robot.NomRobot)] = "Rapide", [nameof(Robot.Moteur)] = (int)Robot.TypeMoteur.Rapide,
            [nameof(Robot.EchelleLargeur)] = 1.4f, [nameof(Robot.CouleurChassis)] = Colors.Red,
        };
        var a = (Robot)spawner.Poser(new Vector3(4, 0, 0));

        spawner.ConfigRobot = new() { [nameof(Robot.NomRobot)] = "Suiveur", [nameof(Robot.Capteurs)] = (int)Robot.TypeCapteurs.Bas };
        var b = (Robot)spawner.Poser(new Vector3(-4, 0, 0));

        spawner.ConfigRobot = null;
        var c = (Robot)spawner.Poser(new Vector3(0, 0, 6));

        float voie(Robot r) => r.LeftWheel.Position.DistanceTo(r.RightWheel.Position);
        bool ok = a.NomRobot == "Rapide" && a.Moteur == Robot.TypeMoteur.Rapide && a.CouleurChassis == Colors.Red
            && b.NomRobot == "Suiveur" && b.Capteurs == Robot.TypeCapteurs.Bas && b.Moteur == Robot.TypeMoteur.Normal
            && c.NomRobot == "Constructeur" && c.Moteur == Robot.TypeMoteur.Lent
            && voie(a) > voie(b) + 0.3f                        // formes de collision propres à chaque robot
            && a.GlobalPosition.IsEqualApprox(new Vector3(4, 0.1f, 0))
            && GetTree().GetNodesInGroup(Robot.Groupe).Count == 3;

        GD.Print(ok ? "TestPoseRobots OK" : $"TestPoseRobots ECHEC : {a.NomRobot}/{a.Moteur}, {b.NomRobot}/{b.Capteurs}/{b.Moteur}, "
                 + $"{c.NomRobot}/{c.Moteur}, voies {voie(a):0.00}/{voie(b):0.00}, position {a.GlobalPosition}");
        GetTree().Quit(ok ? 0 : 1);
    }
}
