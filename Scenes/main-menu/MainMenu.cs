using Godot;
using RobotMaker.Scripts;

namespace RobotMaker.Menu;

public partial class MainMenu : Control
{
    private const string SceneConstructeur = "res://Scenes/constructor.tscn";

    private Control _accueil, _nommer, _charger;
    private LineEdit _nom;
    private Button _creer;
    private Label _aideNom;
    private string _texteAideNom;

    public override void _Ready()
    {
        _accueil = GetNode<Control>("%Accueil");
        _nommer = GetNode<Control>("%Nommer");
        _charger = GetNode<Control>("%Charger");
        _nom = GetNode<LineEdit>("%Nom");
        _creer = GetNode<Button>("%Creer");
        _aideNom = GetNode<Label>("%AideNom");
        _texteAideNom = _aideNom.Text;

        _nom.TextChanged += VerifierNom;
        _nom.TextSubmitted += _ => Creer();
        _creer.Pressed += Creer;
        GetNode<Button>("%Retour").Pressed += () => Afficher(_accueil);
        GetNode<Button>("%RetourCharger").Pressed += () => Afficher(_accueil);
    }

    // Nouveau robot : on demande d'abord son nom
    public void On_newRobot_pressed()
    {
        Afficher(_nommer);
        _nom.GrabFocus();
    }

    public void On_loadRobot_pressed()
    {
        RemplirListe();
        Afficher(_charger);
    }

    public void On_quit_pressed() => GetTree().Quit();

    private void Afficher(Control vue)
    {
        foreach (Control v in new[] { _accueil, _nommer, _charger })
            v.Visible = v == vue;
    }

    private void VerifierNom(string texte)
    {
        string nom = texte.StripEdges();
        _creer.Disabled = nom == "";
        _aideNom.Text = nom != "" && Sauvegarde.Existe(nom)
            ? "Un robot porte déjà ce nom (ou un nom qui donne le même fichier, : / ? * deviennent _) : il sera remplacé à la sauvegarde."
            : _texteAideNom;
    }

    // Repart des réglages de la scène, avec juste le nom choisi
    private void Creer()
    {
        string nom = _nom.Text.StripEdges();
        if (nom == "")
            return;

        Robot.Choix = new() { [nameof(Robot.NomRobot)] = nom };
        GetTree().ChangeSceneToFile(SceneConstructeur);
    }

    // Une ligne par robot sauvegardé : son nom pour le charger, ✕ pour le supprimer
    private void RemplirListe()
    {
        var liste = GetNode<VBoxContainer>("%ListeRobots");
        foreach (Node n in liste.GetChildren())
            n.QueueFree();

        var robots = Sauvegarde.Liste();
        GetNode<Control>("%AucunRobot").Visible = robots.Count == 0;

        foreach ((string fichier, string nom) in robots)
        {
            var ligne = new HBoxContainer();
            var charger = new Button { Text = nom, SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 40) };
            var supprimer = new Button { Text = "✕", TooltipText = $"Supprimer « {nom} »", CustomMinimumSize = new Vector2(40, 40) };

            charger.Pressed += () =>
            {
                var choix = Sauvegarde.Lire(fichier);
                if (choix == null)
                {
                    // Fichier illisible : ne pas repartir des réglages par défaut, on écraserait le robot « Robot »
                    charger.Text = $"{nom} (fichier illisible)";
                    charger.Disabled = true;
                    return;
                }
                Robot.Choix = choix;
                GetTree().ChangeSceneToFile(SceneConstructeur);
            };
            // Premier clic : demande confirmation, deuxième clic : supprime
            supprimer.Pressed += () =>
            {
                if (supprimer.Text != "Sûr ?")
                {
                    supprimer.Text = "Sûr ?";
                    // Pas confirmé dans les 3 s : on oublie
                    GetTree().CreateTimer(3).Timeout += () =>
                    {
                        if (IsInstanceValid(supprimer))
                            supprimer.Text = "✕";
                    };
                    return;
                }
                Sauvegarde.Supprimer(fichier);
                RemplirListe();
            };

            ligne.AddChild(charger);
            ligne.AddChild(supprimer);
            liste.AddChild(ligne);
        }
    }
}
