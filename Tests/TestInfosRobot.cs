using Godot;
using RobotMaker.InteractiveMap;
using RobotMaker.Scripts;

namespace RobotMaker.Tests;

// Sur la vraie carte : un clic (souris simulée) sur le robot ouvre ses infos sans le déplacer, un glisser le déplace,
// « Suivre » braque la caméra sur lui pendant qu'il roule, « Supprimer » le retire et arrête le suivi.
// Lancer : godot --headless res://Tests/TestInfosRobot.tscn
public partial class TestInfosRobot : Node
{
    private Node _carte;
    private Robot _robot;
    private FreeCamera _camera;
    private Control _infos;
    private float _temps;
    private int _etape;
    private Vector3 _depart;
    private bool _ok = true;
    private string _detail = "";

    public override void _Ready()
    {
        Robot.Choix = null;
        GetWindow().Size = new Vector2I(1600, 900);   // en headless la fenêtre fait 64 px : le clic tomberait sur le panneau
        _carte = GD.Load<PackedScene>("res://InteractiveMap/InteractiveMap.tscn").Instantiate();
        AddChild(_carte);
        _robot = _carte.GetNode<Robot>("Robot");
        _camera = _carte.GetNode<FreeCamera>("FreeCamera");
        _infos = _carte.GetNode<Control>("SidePanel/Infos");
    }

    private void Verifier(string quoi, bool condition)
    {
        if (!condition && _ok)
            (_ok, _detail) = (false, " " + quoi);
    }

    private void Souris(Vector2 position, bool? appui)
    {
        InputEvent e = appui is bool presse
            ? new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = presse, Position = position, GlobalPosition = position,
                ButtonMask = presse ? MouseButtonMask.Left : 0 }
            : new InputEventMouseMotion { Position = position, GlobalPosition = position, ButtonMask = MouseButtonMask.Left };
        GetViewport().PushInput(e);
    }

    public override void _PhysicsProcess(double delta)
    {
        _temps += (float)delta;
        switch (_etape)
        {
            case 0 when _temps > 0.5f:
                // clic sur place au centre du robot (en pause au départ)
                Vector2 ecran = _camera.UnprojectPosition(_robot.ToGlobal(_robot.CenterOfMass));
                _depart = _robot.GlobalPosition;
                Souris(ecran, true);
                Souris(ecran + new Vector2(2, 1), null);   // tremblement de la main, sous le seuil
                Souris(ecran, false);
                _etape = 1;
                break;
            case 1:
                Verifier("infos non affichées", _infos.Visible);
                Verifier("robot déplacé par un simple clic", _robot.GlobalPosition.DistanceTo(_depart) < 0.01f && _robot.Freeze);
                Verifier("nom absent", _carte.GetNode<Label>("SidePanel/Infos/Marge/Boite/Haut/InfosNom").Text == _robot.NomRobot);
                // vrai glisser : 40 px vers la droite
                Vector2 a = _camera.UnprojectPosition(_robot.ToGlobal(_robot.CenterOfMass));
                Souris(a, true);
                Souris(a + new Vector2(20, 0), null);
                Souris(a + new Vector2(40, 0), null);
                Souris(a + new Vector2(40, 0), false);
                _etape = 10;
                break;
            case 10:
                Verifier("le glisser n'a pas déplacé le robot", _robot.GlobalPosition.X > _depart.X + 0.3f);
                _carte.GetNode<Button>("SidePanel/Infos/Marge/Boite/BoutonsInfos/Suivre").ButtonPressed = true;
                _carte.GetNode<Button>("SidePanel/Panel/Marge/Boite/BoutonsRobot/Demarrer").ButtonPressed = true;
                Verifier("caméra ne suit pas", _camera.Suivi == _robot);
                (_etape, _temps) = (2, 0f);
                break;
            case 2 when _temps > 3f:
                Vector3 centre = _robot.ToGlobal(_robot.CenterOfMass);
                float vise = (-_camera.GlobalBasis.Z).Normalized().Dot((centre - _camera.GlobalPosition).Normalized());
                Verifier($"caméra pas braquée sur le robot ({vise:0.000})", vise > 0.995f);
                Verifier($"caméra pas à la bonne distance ({_camera.GlobalPosition.DistanceTo(centre):0.0})",
                    Mathf.Abs(_camera.GlobalPosition.DistanceTo(centre) - _camera.DistanceSuivi) < 1f);
                Verifier("le robot n'a pas roulé", _robot.TempsEnMarche > 2f && _robot.DistanceParcourue > 1f);
                GD.Print("  infos : " + _carte.GetNode<Label>("SidePanel/Infos/Marge/Boite/InfosTexte").Text.Replace("\n", " | "));
                _carte.GetNode<Button>("SidePanel/Infos/Marge/Boite/BoutonsInfos/SupprimerRobot").EmitSignal(BaseButton.SignalName.Pressed);
                (_etape, _temps) = (3, 0f);
                break;
            case 3 when _temps > 0.2f:
                Verifier("robot pas supprimé", !IsInstanceValid(_robot));
                Verifier("caméra suit encore", _camera.Suivi == null);
                Verifier("infos encore affichées", !_infos.Visible);
                GD.Print(_ok ? "TestInfosRobot OK" : "TestInfosRobot ECHEC :" + _detail);
                GetTree().Quit(_ok ? 0 : 1);
                break;
        }
    }
}
