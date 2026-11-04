using Godot;
using RobotMaker.Scripts;

public partial class MainMenu : Control
{
	private Control _accueil, _nommer;
	private LineEdit _nom;
	private Button _creer;

	public override void _Ready()
	{
		_accueil = GetNode<Control>("%Accueil");
		_nommer = GetNode<Control>("%Nommer");
		_nom = GetNode<LineEdit>("%Nom");
		_creer = GetNode<Button>("%Creer");

		_nom.TextChanged += texte => _creer.Disabled = texte.StripEdges() == "";
		_nom.TextSubmitted += _ => Creer();
		_creer.Pressed += Creer;
		GetNode<Button>("%Retour").Pressed += () => Afficher(false);
	}

	// Nouveau robot : on demande d'abord son nom
	public void On_newRobot_pressed() => Afficher(true);

	public void On_loadRobot_pressed()
	{
		GD.Print("Load Robot button pressed");
	}

	public void On_quit_pressed()
	{
		GetTree().Quit();
	}

	private void Afficher(bool nommer)
	{
		_accueil.Visible = !nommer;
		_nommer.Visible = nommer;
		if (nommer)
			_nom.GrabFocus();
	}

	// Repart des réglages de la scène, avec juste le nom choisi
	private void Creer()
	{
		string nom = _nom.Text.StripEdges();
		if (nom == "")
			return;

		TestRobot.Choix = new() { [nameof(TestRobot.NomRobot)] = nom };
		GetTree().ChangeSceneToFile("res://Scenes/constructor.tscn");
	}
}
