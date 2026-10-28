using Godot;
using System;

namespace RobotMaker.Scripts;

[GlobalClass, Tool]
public partial class ConstructorUI : CanvasLayer
{
	private const string Panneau = "Scroll/VBoxContainer/";

	private Constructor _constructeur;

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
			return;

		_constructeur = GetParent<Constructor>();
		Brancher("Body", _constructeur.ChangerCouleurChassis);
		Brancher("WheelColor", _constructeur.ChangerCouleurRoues);
		Brancher("Wheels", _constructeur.ChangerRoueAvant);
		Brancher("Motor", _constructeur.ChangerMoteur);
		Brancher("Sensor", _constructeur.ChangerCapteurs);

		// Chaque curseur porte le nom de la propriété du robot qu'il règle
		foreach (Node n in GetNode(Panneau).FindChildren("Curseur", nameof(HSlider), true, false))
			BrancherCurseur((HSlider)n);

		GetNode<Button>(Panneau + "Play").Pressed += _constructeur.Jouer;
	}

	// Relie Previous / Next d'une ligne à son choix et affiche le nom dans le label Choix
	private void Brancher(string ligne, Func<int, string> changer)
	{
		string chemin = Panneau + ligne + "/HBoxContainer/";
		var choix = GetNode<Label>(chemin + "Choix");
		choix.Text = changer(0);
		GetNode<Button>(chemin + "Previous").Pressed += () => choix.Text = changer(-1);
		GetNode<Button>(chemin + "Next").Pressed += () => choix.Text = changer(1);
	}

	// Ligne de curseur : son nom est celui de la propriété du robot, la valeur s'affiche à droite
	private void BrancherCurseur(HSlider curseur)
	{
		string reglage = curseur.GetMeta("reglage").AsString();
		var texte = curseur.GetParent().GetNode<Label>("Valeur");

		curseur.Value = _constructeur.Robot.Get(reglage).AsDouble();
		texte.Text = $"{curseur.Value:0.##}";
		curseur.ValueChanged += v =>
		{
			texte.Text = $"{v:0.##}";
			_constructeur.Changer(reglage, v);
		};
	}
}
