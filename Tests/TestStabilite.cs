using Godot;
using RobotMaker.Scripts;

namespace RobotMaker.Tests;

// Robot au repos puis conduit sans ménagement (moteur Rapide, au clavier) : il doit se poser à plat
// à la bonne hauteur, et ne jamais basculer en accélérant, en virage, au freinage ni en marche arrière.
// Lancer : godot --headless --fixed-fps 60 res://Tests/TestStabilite.tscn
public partial class TestStabilite : Node3D
{
    // Hauteur de l'origine du robot au repos, mesurée avant le passage de la gravité à l'échelle
    private const float HauteurRepos = -0.010f;

    private Robot _robot;
    private float _temps, _penteMax, _vitesseMax;
    private string _phase = "";
    private bool _ok = true;
    private string _detail = "";

    public override void _Ready()
    {
        _robot = GetNode<Robot>("Robot");
        _robot.Set(nameof(Robot.Moteur), (int)Robot.TypeMoteur.Rapide);
        _robot.Appliquer();
    }

    private void Touches(params string[] actions)
    {
        foreach (string a in new[] { "up", "down", "left", "right" })
            if (System.Array.IndexOf(actions, a) >= 0) Input.ActionPress(a); else Input.ActionRelease(a);
    }

    public override void _PhysicsProcess(double delta)
    {
        _temps += (float)delta;
        float pente = Mathf.RadToDeg(Mathf.Acos(Mathf.Clamp(_robot.GlobalBasis.Y.Normalized().Y, -1f, 1f)));
        _vitesseMax = Mathf.Max(_vitesseMax, _robot.LinearVelocity.Length());

        if (_temps < 2f)
            return;   // se pose
        if (_phase == "")
        {
            float y = _robot.GlobalPosition.Y;
            GD.Print($"  repos : hauteur {y:0.000} (attendu {HauteurRepos:0.000}), pente {pente:0.0}°, vitesse {_robot.LinearVelocity.Length():0.000}");
            if (Mathf.Abs(y - HauteurRepos) > 0.03f || pente > 2f || _robot.LinearVelocity.Length() > 0.05f)
                (_ok, _detail) = (false, _detail + " repos");
            _phase = "accélère";
        }

        _penteMax = Mathf.Max(_penteMax, pente);
        string phase = _temps switch
        {
            < 4f => "accélère",
            < 6f => "virage à fond",
            < 7f => "freine",
            < 8.5f => "marche arrière",
            < 9f => "freine en reculant",
            < 11f => "tourne sur place",
            _ => "fin",
        };
        if (phase != _phase)
        {
            GD.Print($"  {_phase} : pente max {_penteMax:0.0}°, vitesse max {_vitesseMax:0.00}");
            if (_penteMax > 15f)
                (_ok, _detail) = (false, _detail + $" {_phase} ({_penteMax:0}°)");
            (_phase, _penteMax, _vitesseMax) = (phase, 0f, 0f);
        }

        switch (_phase)
        {
            case "accélère": Touches("up"); break;
            case "virage à fond": Touches("up", "left"); break;
            case "freine": Touches(); break;
            case "marche arrière": Touches("down"); break;
            case "freine en reculant": Touches(); break;
            case "tourne sur place": Touches("right"); break;
            default:
                Touches();
                GD.Print(_ok ? "TestStabilite OK" : "TestStabilite ECHEC :" + _detail);
                GetTree().Quit(_ok ? 0 : 1);
                break;
        }
    }
}
