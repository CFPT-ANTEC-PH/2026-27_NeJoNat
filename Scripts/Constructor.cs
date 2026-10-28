using Godot;
using System;

namespace RobotMaker.Scripts;

// Aperçu du robot : chaque réglage est appliqué tout de suite, et gardé pour la scène de jeu en cliquant sur Play
public partial class Constructor : Node3D
{
	[Export] public TestRobot Robot;
	[Export(PropertyHint.File, "*.tscn")] public string SceneJeu = "res://Scenes/world.tscn";

	private static readonly (string Nom, Color Couleur)[] CouleursChassis =
	{
		("Bleu ciel", new Color(0.62f, 0.81f, 0.93f)),
		("Rouge", new Color(0.85f, 0.15f, 0.15f)),
		("Vert", new Color(0.2f, 0.7f, 0.3f)),
		("Jaune", new Color(0.95f, 0.8f, 0.1f)),
		("Orange", new Color(1f, 0.5f, 0.1f)),
		("Violet", new Color(0.55f, 0.25f, 0.8f)),
		("Blanc", new Color(0.95f, 0.95f, 0.95f)),
		("Noir", new Color(0.1f, 0.1f, 0.1f)),
	};

	private static readonly (string Nom, Color Couleur)[] CouleursRoues =
	{
		("Origine", Colors.Transparent),
		("Noir", new Color(0.08f, 0.08f, 0.08f)),
		("Gris", new Color(0.45f, 0.45f, 0.48f)),
		("Rouge", new Color(0.85f, 0.15f, 0.15f)),
		("Bleu", new Color(0.15f, 0.35f, 0.9f)),
		("Jaune", new Color(0.95f, 0.8f, 0.1f)),
		("Blanc", new Color(0.95f, 0.95f, 0.95f)),
	};

	// Change un réglage du robot (nom de sa propriété) et met l'aperçu à jour
	public void Changer(string reglage, Variant valeur)
	{
		Robot.Set(reglage, valeur);
		Robot.Appliquer();
	}

	// sens : -1 précédent, +1 suivant, 0 juste lire le choix actuel. Renvoient le nom à afficher.
	public string ChangerRoueAvant(int sens) => ChangerEnum(nameof(TestRobot.RoueAvant), Robot.RoueAvant, sens);
	public string ChangerCapteurs(int sens) => ChangerEnum(nameof(TestRobot.Capteurs), Robot.Capteurs, sens);
	public string ChangerMoteur(int sens) => ChangerEnum(nameof(TestRobot.Moteur), Robot.Moteur, sens);
	public string ChangerCouleurChassis(int sens) => ChangerCouleur(nameof(TestRobot.CouleurChassis), CouleursChassis, sens);
	public string ChangerCouleurRoues(int sens) => ChangerCouleur(nameof(TestRobot.CouleurRoues), CouleursRoues, sens);

	public void Jouer()
	{
		Robot.Sauver();
		GetTree().ChangeSceneToFile(SceneJeu);
	}

	// Valeur suivante ou précédente de l'enum, en boucle
	private string ChangerEnum<T>(string reglage, T actuel, int sens) where T : struct, Enum
	{
		T[] valeurs = Enum.GetValues<T>();
		T choix = valeurs[(Array.IndexOf(valeurs, actuel) + sens + valeurs.Length) % valeurs.Length];
		Changer(reglage, Convert.ToInt32(choix));
		return choix.ToString();
	}

	// Couleur suivante de la liste (une couleur réglée hors liste dans l'inspecteur repart de la première)
	private string ChangerCouleur(string reglage, (string Nom, Color Couleur)[] liste, int sens)
	{
		Color actuelle = Robot.Get(reglage).AsColor();
		int i = Array.FindIndex(liste, c => c.Couleur.IsEqualApprox(actuelle));
		i = i < 0 ? 0 : (i + sens + liste.Length) % liste.Length;
		Changer(reglage, liste[i].Couleur);
		return liste[i].Nom;
	}
}
