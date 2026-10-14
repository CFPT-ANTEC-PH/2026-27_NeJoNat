using Godot;

namespace RobotMaker.InteractiveMap;

// Menu de gauche. La mise en page est dans SidePanel.tscn ; ici on crée
// un bouton par scène de la liste Objets et on branche les boutons.
public partial class SidePanel : CanvasLayer
{
    [Export] public Godot.Collections.Array<PackedScene> Objets = new();
    [Export] public LineDrawer Ligne;
    [Export] public ObjectSpawner Spawner;

    private Label _mode;

    public override void _Ready()
    {
        _mode = GetNode<Label>("%Mode");

        var liste = GetNode<VBoxContainer>("%ListeObjets");
        foreach (PackedScene scene in Objets)
        {
            string nom = scene.ResourcePath.GetFile().GetBaseName();
            var bouton = new Button { Text = nom, FocusMode = Control.FocusModeEnum.None };
            bouton.Pressed += () => Choisir(scene, false, nom);
            liste.AddChild(bouton);
        }

        GetNode<Button>("%ViderObjets").Pressed += () => Spawner.Vider();
        GetNode<Button>("%Dessiner").Pressed += () => Choisir(null, true, "Dessin");
        GetNode<Button>("%EffacerLigne").Pressed += () => Ligne.Effacer();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("ui_cancel"))
            Choisir(null, false, "Aucun");
    }

    private void Choisir(PackedScene objet, bool dessin, string nom)
    {
        Spawner.Selection = objet;
        Ligne.Actif = dessin;
        _mode.Text = "Outil : " + nom;
    }
}
