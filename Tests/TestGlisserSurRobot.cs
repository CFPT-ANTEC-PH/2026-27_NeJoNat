using Godot;
using System.Linq;
using RobotMaker.InteractiveMap;
using RobotMaker.Scripts;

namespace RobotMaker.Tests;

// On glisse le robot A à travers le robot B (sur plusieurs images, comme à la souris) puis on le lâche plus loin :
// rien ne doit devenir NaN, B ne doit pas être bousculé et A doit repartir. Ensuite on lâche A sur B :
// il doit revenir là où on l'a pris. Lancer : godot --headless res://Tests/TestGlisserSurRobot.tscn
public partial class TestGlisserSurRobot : Node3D
{
    private Robot _a, _b;
    private DeplaceurRobots _deplaceur;
    private float _temps;
    private int _etape;
    private Vector3 _point, _prise;
    private bool _ok = true;
    private string _detail = "";

    public override void _Ready()
    {
        _a = GetNode<Robot>("A");
        _b = GetNode<Robot>("B");
        _deplaceur = GetNode<DeplaceurRobots>("DeplaceurRobots");
    }

    private void Verifier(string quoi, bool condition)
    {
        if (!condition && _ok)
            (_ok, _detail) = (false, $" {quoi} (A {_a.GlobalPosition}, B {_b.GlobalPosition})");
    }

    private static bool ToutFini(Node n) =>
        (n is not Node3D n3 || n3.GlobalTransform.IsFinite()) && n.GetChildren().All(ToutFini);

    private static float APlat(Vector3 a, Vector3 b) => new Vector2(a.X - b.X, a.Z - b.Z).Length();

    public override void _PhysicsProcess(double delta)
    {
        _temps += (float)delta;
        Verifier("transformation NaN", ToutFini(_a) && ToutFini(_b));

        switch (_etape)
        {
            case 0 when _temps > 1f:   // saisit A et le glisse vers +X, à travers B
                _point = new Vector3(_a.GlobalPosition.X, 0f, _a.GlobalPosition.Z + 1f);
                _deplaceur.Prendre(_a, _point);
                _etape = 1;
                break;
            case 1:
                _point.X += 0.1f;
                _deplaceur.Bouger(_point);
                if (_point.X > 8f)
                {
                    _deplaceur.Lacher();
                    (_etape, _temps) = (2, 0f);
                }
                break;
            case 2 when _temps > 1.5f:
                Verifier("B bousculé", Mathf.Abs(_b.GlobalPosition.X) < 0.05f && _b.GlobalBasis.Y.Y > 0.99f);
                Verifier("A ne repart pas", _a.LinearVelocity.Length() > 0.3f && !_a.Freeze);
                // lâche A en plein sur B
                _prise = _a.GlobalPosition;
                _deplaceur.Prendre(_a, new Vector3(_a.GlobalPosition.X, 0f, _a.GlobalPosition.Z));
                _deplaceur.Bouger(new Vector3(_b.GlobalPosition.X, 0f, _b.GlobalPosition.Z));
                _deplaceur.Lacher();
                Verifier("lâché sur B : pas revenu", APlat(_a.GlobalPosition, _prise) < 0.1f);
                (_etape, _temps) = (3, 0f);
                break;
            case 3 when _temps > 1f:
                Verifier("A ne repart pas après le refus", _a.LinearVelocity.Length() > 0.3f);
                GD.Print(_ok ? "TestGlisserSurRobot OK" : "TestGlisserSurRobot ECHEC :" + _detail);
                GetTree().Quit(_ok ? 0 : 1);
                break;
        }
    }
}
