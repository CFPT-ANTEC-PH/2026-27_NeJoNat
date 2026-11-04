using Godot;
using System.Linq;
using RobotMaker.Scripts;

namespace RobotMaker.Tests;

// Vérifie l'aller-retour d'une sauvegarde. Lancer : godot --headless res://Tests/TestSauvegarde.tscn
public partial class TestSauvegarde : Node
{
    public override void _Ready()
    {
        const string nom = "Test: robot/1";   // caractères interdits volontaires
        Sauvegarde.Ecrire(new()
        {
            [nameof(Robot.NomRobot)] = nom,
            [nameof(Robot.CouleurChassis)] = Colors.Red,
            [nameof(Robot.Capteurs)] = (int)Robot.TypeCapteurs.Bas,
            ["mass"] = 2.5f,
        });

        string fichier = Sauvegarde.Fichier(nom);
        var lu = Sauvegarde.Lire(fichier);
        bool ok = Sauvegarde.Existe(nom)
            && lu[nameof(Robot.NomRobot)].AsString() == nom
            && lu[nameof(Robot.CouleurChassis)].AsColor().IsEqualApprox(Colors.Red)
            && lu[nameof(Robot.Capteurs)].AsInt32() == (int)Robot.TypeCapteurs.Bas
            && Mathf.IsEqualApprox(lu["mass"].AsSingle(), 2.5f)
            && Sauvegarde.Liste().Any(r => r.Nom == nom);

        Sauvegarde.Supprimer(fichier);
        ok &= !Sauvegarde.Existe(nom);

        GD.Print(ok ? "TestSauvegarde OK" : "TestSauvegarde ECHEC");
        GetTree().Quit(ok ? 0 : 1);
    }
}
