using Godot;
using System.Linq;
using RobotMaker.Scripts;

// Vérifie l'aller-retour d'une sauvegarde. Lancer : godot --headless res://Tests/TestSauvegarde.tscn
public partial class TestSauvegarde : Node
{
    public override void _Ready()
    {
        const string nom = "Test: robot/1";   // caractères interdits volontaires
        Sauvegarde.Ecrire(new()
        {
            [nameof(TestRobot.NomRobot)] = nom,
            [nameof(TestRobot.CouleurChassis)] = Colors.Red,
            [nameof(TestRobot.Capteurs)] = (int)TestRobot.TypeCapteurs.Bas,
            ["mass"] = 2.5f,
        });

        string fichier = Sauvegarde.Fichier(nom);
        var lu = Sauvegarde.Lire(fichier);
        bool ok = Sauvegarde.Existe(nom)
            && lu[nameof(TestRobot.NomRobot)].AsString() == nom
            && lu[nameof(TestRobot.CouleurChassis)].AsColor().IsEqualApprox(Colors.Red)
            && lu[nameof(TestRobot.Capteurs)].AsInt32() == (int)TestRobot.TypeCapteurs.Bas
            && Mathf.IsEqualApprox(lu["mass"].AsSingle(), 2.5f)
            && Sauvegarde.Liste().Any(r => r.Nom == nom);

        Sauvegarde.Supprimer(fichier);
        ok &= !Sauvegarde.Existe(nom);

        GD.Print(ok ? "TestSauvegarde OK" : "TestSauvegarde ECHEC");
        GetTree().Quit(ok ? 0 : 1);
    }
}
