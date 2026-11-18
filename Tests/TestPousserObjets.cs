using Godot;
using System.Collections.Generic;
using RobotMaker.Scripts;

namespace RobotMaker.Tests;

// Le robot avance tout droit dans chaque objet de la carte (cube, cylindre, balle) : il doit le pousser sans se bloquer,
// même dans les réglages les plus défavorables (robot le plus léger : peu d'adhérence ; moteur lent ou rapide).
// Avant : la régulation lisait une vitesse d'avant les contacts, le robot ne poussait qu'aux 2/3 de sa vitesse.
// Lancer : godot --headless --fixed-fps 60 res://Tests/TestPousserObjets.tscn
public partial class TestPousserObjets : Node3D
{
    private static readonly string[] Objets = { "Cube", "Cylindre", "Sphere" };
    private static readonly (float Masse, Robot.TypeMoteur Moteur)[] Robots =
    {
        (1f, Robot.TypeMoteur.Normal), (0.5f, Robot.TypeMoteur.Lent), (0.5f, Robot.TypeMoteur.Rapide), (3f, Robot.TypeMoteur.Lent),
    };
    private const float Duree = 12f;

    private readonly List<(string Nom, Robot Robot, RigidBody3D Objet, float Libre)> _essais = new();
    private float _temps;

    public override void _Ready()
    {
        var sol = new StaticBody3D { CollisionLayer = 9 };
        sol.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(200, 1, 100) }, Position = Vector3.Down * 0.5f });
        AddChild(sol);

        float x = -60f;
        foreach ((float masse, Robot.TypeMoteur moteur) in Robots)
            foreach (string nom in Objets)
            {
                var robot = GD.Load<PackedScene>("res://Scenes/robot.tscn").Instantiate<Robot>();
                robot.Capteurs = Robot.TypeCapteurs.Bas;   // pas d'anti-collision : il avance tout droit, ligne pas trouvée
                robot.Moteur = moteur;
                robot.Mass = masse;
                robot.Position = new Vector3(x, 0.1f, 0);
                AddChild(robot);   // _Ready applique le moteur : MaxSpeed est alors celle de ce moteur

                var objet = GD.Load<PackedScene>($"res://InteractiveMap/Objects/{nom}.tscn").Instantiate<RigidBody3D>();
                float demiHauteur = -objet.GetNode<MeshInstance3D>("MeshInstance3D").GetAabb().Position.Y;
                objet.Position = new Vector3(x, demiHauteur + 0.05f, 5);
                AddChild(objet);

                // Distance faite sans objet : vitesse de recherche de ligne (VitesseAuto / 2), moins ~0,5 s de démarrage
                float libre = robot.VitesseAuto * 0.5f * robot.MaxSpeed * (Duree - 0.5f);
                _essais.Add(($"{nom}, robot {masse} kg {moteur}", robot, objet, libre));
                x += 10f;
            }
    }

    public override void _PhysicsProcess(double delta)
    {
        _temps += (float)delta;
        if (_temps < Duree)
            return;

        // Poussé = il avance au moins aux 70 % de sa vitesse sans objet, l'objet toujours devant lui.
        // Le robot de 0,5 kg contre le cylindre est à la limite d'adhérence (3,1 N à vaincre, ~3,4 N au sol) : il patine un peu, ~83 %
        bool ok = true;
        foreach ((string nom, Robot robot, RigidBody3D objet, float libre) in _essais)
        {
            float z = robot.GlobalPosition.Z, zo = objet.GlobalPosition.Z;
            bool pousse = z > 0.7f * libre && zo > z;
            ok &= pousse;
            GD.Print($"  {nom} : robot à {z:0.0} u (sans objet ~{libre:0.0}), objet à {zo:0.0} u, {(pousse ? "poussé" : "BLOQUÉ")}");
        }
        GD.Print($"TestPousserObjets {(ok ? "OK" : "ECHEC")}");
        GetTree().Quit(ok ? 0 : 1);
    }
}
