using Godot;
using System.Collections.Generic;
using RobotMaker.Scripts;

namespace RobotMaker.InteractiveMap;

// Menu de gauche. La mise en page est dans SidePanel.tscn ; ici on crée
// un bouton par scène de la liste Objets et on branche les boutons.
public partial class SidePanel : CanvasLayer
{
    [Export] public Godot.Collections.Array<PackedScene> Objets = new();
    [Export] public LineDrawer Ligne;
    [Export] public ObjectSpawner Spawner;
    [Export] public PackedScene SceneRobot;

    private Label _mode;
    private readonly List<Button> _outils = new();   // boutons à bascule : celui de l'outil actif reste enfoncé

    public override void _Ready()
    {
        _mode = GetNode<Label>("%Mode");

        var liste = GetNode<HBoxContainer>("%ListeObjets");
        foreach (PackedScene scene in Objets)
        {
            string nom = scene.ResourcePath.GetFile().GetBaseName().Capitalize();
            var bouton = new Button { Text = nom, FocusMode = Control.FocusModeEnum.None, ToggleMode = true,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            bouton.Pressed += () => Choisir(scene, false, nom, bouton);
            liste.AddChild(bouton);
            _outils.Add(bouton);
        }

        var dessiner = GetNode<Button>("%Dessiner");
        _outils.Add(dessiner);
        dessiner.Pressed += () => Choisir(null, true, "Dessin de ligne", dessiner);

        GetNode<Button>("%ViderObjets").Pressed += () => Spawner.Vider();
        GetNode<Button>("%EffacerLigne").Pressed += () => Ligne.Effacer();
        GetNode<Button>("%Constructeur").Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/constructor.tscn");
        Choisir(null, false, "Aucun", null);

        BrancherPoseRobot();

        // Les robots attendent « Démarrer » : on peut d'abord tracer la ligne et poser les obstacles
        var demarrer = GetNode<Button>("%Demarrer");
        demarrer.Toggled += Marche;
        Marche(false);
        GetNode<Button>("%Replacer").Pressed += () =>
        {
            foreach (Node n in GetTree().GetNodesInGroup(Robot.Groupe))
                ((Robot)n).Replacer();
        };

        void Marche(bool enMarche)
        {
            Robot.Arret = !enMarche;
            demarrer.Text = enMarche ? "⏸ Pause" : "▶ Démarrer";
            foreach (Node n in GetTree().GetNodesInGroup(Robot.Groupe))
                ((Robot)n).Freeze = !enMarche;
        }
    }

    // Liste des robots sauvegardés : celui choisi est posé au clic, avec ses propres réglages
    private void BrancherPoseRobot()
    {
        var choix = GetNode<OptionButton>("%ChoixRobot");
        var poser = GetNode<Button>("%PoserRobot");
        var configs = new List<Dictionary<string, Variant>>();

        string actuel = Robot.Choix != null && Robot.Choix.TryGetValue(nameof(Robot.NomRobot), out Variant n) ? n.AsString() : null;
        foreach ((string fichier, string nom) in Sauvegarde.Liste())
        {
            if (Sauvegarde.Lire(fichier) is not { } config)
                continue;
            choix.AddItem(nom);
            configs.Add(config);
            if (nom == actuel)
                choix.Select(configs.Count - 1);
        }
        if (configs.Count == 0)
        {
            choix.AddItem("Robot du constructeur");
            configs.Add(null);
        }

        void Prendre(long i)
        {
            Spawner.ConfigRobot = configs[(int)i];
            if (poser.ButtonPressed)
                Choisir(SceneRobot, false, $"Poser « {choix.Text} »", poser);
        }
        Prendre(choix.Selected);
        choix.ItemSelected += Prendre;

        _outils.Add(poser);
        poser.Pressed += () => Choisir(SceneRobot, false, $"Poser « {choix.Text} »", poser);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("ui_cancel"))
            Choisir(null, false, "Aucun", null);
    }

    // Recliquer sur l'outil actif le désactive
    private void Choisir(PackedScene objet, bool dessin, string nom, Button bouton)
    {
        if (bouton != null && !bouton.ButtonPressed)
            (objet, dessin, nom, bouton) = (null, false, "Aucun", null);

        Spawner.Selection = objet;
        Ligne.Actif = dessin;
        _mode.Text = "Outil : " + nom;
        _mode.SelfModulate = bouton == null ? new Color(1f, 1f, 1f, 0.35f) : Colors.White;
        foreach (Button b in _outils)
            b.SetPressedNoSignal(b == bouton);
    }
}
