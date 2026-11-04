using Godot;
using System;
using System.Collections.Generic;

namespace RobotMaker.Scripts;

public partial class ConstructorUI : CanvasLayer
{
    private const string Panneau = "%Reglages/";

    // Nom affiché et explication de chaque valeur d'enum (les couleurs s'affichent telles quelles)
    private static readonly Dictionary<string, (string Nom, string Aide)> Textes = new()
    {
        ["RoueFolle"] = ("Roue folle", "Petite roue qui pivote toute seule dans le sens du déplacement."),
        ["Glisseur"] = ("Glisseur", "Patin qui glisse au sol sans accrocher : simple, aucune résistance en virage."),
        ["Lent"] = ("Lent", "60 % de la vitesse et de la force : précis, idéal pour suivre une ligne serrée."),
        ["Normal"] = ("Normal", "Vitesse et force de base."),
        ["Rapide"] = ("Rapide", "160 % de la vitesse et de la force : rapide, mais plus dur à contrôler."),
        ["Avant"] = ("Capteurs avant", "3 capteurs d'obstacle : le robot explore la carte et contourne murs et objets."),
        ["Bas"] = ("Capteurs bas", "5 capteurs de ligne : le robot suit la ligne noire dessinée sur la carte."),
        ["Auto"] = ("Automatique", "Le robot décide seul à partir de ses capteurs."),
        ["Manuel"] = ("Manuel", "Tu le conduis au clavier : ZQSD / WASD."),
    };

    private Constructor _constructeur;

    public override void _Ready()
    {
        _constructeur = GetParent<Constructor>();
        GetNode<Label>("Panneau/Marge/Contenu/SousTitre").Text =
            $"Règle « {_constructeur.Robot.NomRobot} » : l'aperçu change en direct, la sauvegarde est automatique.";
        Brancher("Body", _constructeur.ChangerCouleurChassis);
        Brancher("WheelColor", _constructeur.ChangerCouleurRoues);
        Brancher("Wheels", _constructeur.ChangerRoueAvant);
        Brancher("Motor", _constructeur.ChangerMoteur);
        Brancher("Pilot", _constructeur.ChangerPilotage);
        Brancher("Sensor", sens =>
        {
            string choix = _constructeur.ChangerCapteurs(sens);
            // Portée et distance d'arrêt ne servent qu'aux capteurs avant
            bool avant = _constructeur.Robot.Capteurs == Robot.TypeCapteurs.Avant;
            GetNode<Control>(Panneau + "Range").Visible = avant;
            GetNode<Control>(Panneau + "StopDistance").Visible = avant;
            return choix;
        });

        // Chaque curseur porte le nom de la propriété du robot qu'il règle
        foreach (Node n in GetNode(Panneau).FindChildren("Curseur", nameof(HSlider), true, false))
            BrancherCurseur((HSlider)n);

        GetNode<Button>("%Play").Pressed += _constructeur.Jouer;
        GetNode<Button>("%Menu").Pressed += () =>
        {
            _constructeur.Enregistrer();
            GetTree().ChangeSceneToFile("res://Scenes/main-menu/main-menu.tscn");
        };
    }

    // Relie < / > d'une ligne à son choix, affiche le nom dans Choix et son explication dans Aide
    private void Brancher(string ligne, Func<int, string> changer)
    {
        string chemin = Panneau + ligne + "/HBoxContainer/";
        var choix = GetNode<Label>(chemin + "Choix");
        var aide = GetNode<Label>(Panneau + ligne + "/Aide");

        void Afficher(string valeur)
        {
            if (Textes.TryGetValue(valeur, out var t))
            {
                choix.Text = t.Nom;
                aide.Text = t.Aide;
            }
            else
                choix.Text = valeur;
        }

        Afficher(changer(0));
        GetNode<Button>(chemin + "Previous").Pressed += () => Afficher(changer(-1));
        GetNode<Button>(chemin + "Next").Pressed += () => Afficher(changer(1));
    }

    // Ligne de curseur : sa meta "reglage" est la propriété du robot, "format" l'affichage de la valeur
    private void BrancherCurseur(HSlider curseur)
    {
        string reglage = curseur.GetMeta("reglage").AsString();
        string format = curseur.GetMeta("format", "{0:0.##}").AsString();
        var texte = curseur.GetParent().GetNode<Label>("Valeur");

        curseur.Value = _constructeur.Robot.Get(reglage).AsDouble();
        texte.Text = string.Format(format, curseur.Value);
        curseur.ValueChanged += v =>
        {
            texte.Text = string.Format(format, v);
            _constructeur.Changer(reglage, v);
        };
    }
}
