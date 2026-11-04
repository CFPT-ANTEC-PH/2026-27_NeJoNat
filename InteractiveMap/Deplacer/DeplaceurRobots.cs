using Godot;
using RobotMaker.Scripts;

namespace RobotMaker.InteractiveMap;

// Glisser-déposer des robots : clic gauche sur un robot, glisser, relâcher.
// Un simple clic (sans bouger) sélectionne le robot. Molette pendant le glisser : tourne le robot. Prioritaire sur les autres outils
// (ce nœud est le dernier de la carte, il reçoit donc les clics avant eux).
public partial class DeplaceurRobots : Node3D
{
    [Export(PropertyHint.Layers3DPhysics)] public uint MasqueSol = 8;   // calque 4 : sol de la table
    [Export] public float Levee = 0.5f;          // hauteur du robot pendant qu'on le porte
    [Export] public float PasRotation = 15f;     // degrés par cran de molette

    [Export] public float SeuilGlisser = 6f;     // pixels : en dessous, c'est un clic

    public bool Porte => _robot != null;
    public event System.Action<Robot> Selectionne;   // clic sur un robot, ou robot qu'on vient de lâcher

    private Robot _robot;
    private Robot _clique;       // robot sous le clic, pas encore glissé
    private Vector2 _souris;     // position de la souris au clic
    private Vector3 _point;      // point du sol sous la souris
    private Vector3 _decalage;   // de ce point à l'origine du robot, à plat
    private Transform3D _prise;  // où il était quand on l'a saisi : on l'y remet si le dépôt est impossible
    private uint _calque, _masque;

    public override void _UnhandledInput(InputEvent e)
    {
        if (_robot == null && _clique == null && e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } clic
            && Pick.ObjetSousLaSouris(this, 1, clic.Position) is Robot robot && Pick.SousLaSouris(this, MasqueSol, clic.Position) is Vector3 point)
        {
            (_clique, _souris, _point) = (robot, clic.Position, point);
            GetViewport().SetInputAsHandled();
        }
    }

    // Dans _Input : le relâcher doit être vu même si la souris est au-dessus du panneau
    public override void _Input(InputEvent e)
    {
        if (_clique != null)
        {
            // La souris bouge assez : on commence à glisser. Relâchée sur place : simple clic.
            if (e is InputEventMouseMotion m && m.Position.DistanceTo(_souris) > SeuilGlisser && IsInstanceValid(_clique))
                Prendre(_clique, _point);
            else if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
            {
                if (IsInstanceValid(_clique))
                    Selectionne?.Invoke(_clique);
                _clique = null;
                GetViewport().SetInputAsHandled();
                return;
            }
            else
                return;
            _clique = null;
        }

        if (_robot == null)
            return;

        if (e is InputEventMouseMotion souris)
        {
            if (Pick.SousLaSouris(this, MasqueSol, souris.Position) is Vector3 point)
                Bouger(point);
        }
        else if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } molette)
            Tourner(molette.ButtonIndex == MouseButton.WheelUp ? PasRotation : -PasRotation);
        else if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
            Lacher();
        else
            return;

        GetViewport().SetInputAsHandled();   // ni ligne tracée, ni zoom caméra pendant le glisser
    }

    public void Prendre(Robot robot, Vector3 point)
    {
        _robot = robot;
        _robot.Freeze = true;   // porté : la physique ne le fait plus tomber
        _prise = robot.GlobalTransform;
        // Sans collisions pendant qu'on le porte : il passe à travers les autres sans les bousculer
        (_calque, _masque) = (robot.CollisionLayer, robot.CollisionMask);
        robot.CollisionLayer = robot.CollisionMask = 0;
        _decalage = robot.GlobalPosition - point;
        _decalage.Y = 0f;
        Input.SetDefaultCursorShape(Input.CursorShape.Drag);
        Bouger(point);
    }

    public void Bouger(Vector3 point)
    {
        _point = point;
        _robot.GlobalPosition = point + _decalage + Vector3.Up * Levee;
    }

    // Tourne autour du point saisi, qui reste sous la souris
    public void Tourner(float degres)
    {
        float angle = Mathf.DegToRad(degres);
        _robot.GlobalBasis = new Basis(Vector3.Up, angle) * _robot.GlobalBasis;
        _decalage = _decalage.Rotated(Vector3.Up, angle);
        Bouger(_point);
    }

    public void Lacher()
    {
        Transform3D t = _robot.GlobalTransform;
        t.Origin.Y = _point.Y + 0.1f;   // comme un robot posé : juste au-dessus du sol
        (_robot.CollisionLayer, _robot.CollisionMask) = (_calque, _masque);

        if (_robot.Chevauche(t))
        {
            // Lâché sur un autre robot, un objet ou un mur : retour là où on l'a pris
            _robot.GlobalTransform = _prise;
            _robot.Freeze = Robot.Arret;
        }
        else
            _robot.Deposer(t);
        Selectionne?.Invoke(_robot);
        _robot = null;
        Input.SetDefaultCursorShape();
    }
}
