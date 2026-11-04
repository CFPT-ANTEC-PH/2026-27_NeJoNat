using Godot;
using RobotMaker.Scripts;

namespace RobotMaker.InteractiveMap;

// Caméra libre : clic droit maintenu pour regarder et bouger (WASD/ZQSD),
// E/Q (touches physiques) monter/descendre, Shift pour aller vite, molette pour régler la vitesse.
// Sans clic droit, le clavier reste au robot en pilotage manuel.
public partial class FreeCamera : Camera3D
{
    [Export] public float Vitesse = 10f;
    [Export] public float MultiplicateurRapide = 3f;
    [Export] public float Sensibilite = 0.003f;

    // Robot suivi : la caméra reste braquée sur lui, le clic droit tourne autour, la molette règle la distance
    public Robot Suivi
    {
        get => _suivi;
        set
        {
            _suivi = value;
            if (value != null)
                DistanceSuivi = Mathf.Clamp(GlobalPosition.DistanceTo(Centre(value)), 4f, 40f);
        }
    }
    public float DistanceSuivi = 15f;
    private Robot _suivi;

    private static Vector3 Centre(Robot r) => r.ToGlobal(r.CenterOfMass);

    private float _yaw;
    private float _pitch;

    public override void _Ready()
    {
        _yaw = Rotation.Y;
        _pitch = Rotation.X;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseButton bouton)
        {
            if (bouton.ButtonIndex == MouseButton.Right)
                Input.MouseMode = bouton.Pressed ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
            else if (bouton.Pressed && bouton.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {
                bool avant = bouton.ButtonIndex == MouseButton.WheelUp;
                if (_suivi != null)
                    DistanceSuivi = Mathf.Clamp(DistanceSuivi * (avant ? 0.9f : 1.1f), 3f, 60f);
                else
                    Vitesse *= avant ? 1.1f : 1f / 1.1f;
            }
        }
        else if (e is InputEventMouseMotion souris && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _yaw -= souris.Relative.X * Sensibilite;
            _pitch = Mathf.Clamp(_pitch - souris.Relative.Y * Sensibilite, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
            Rotation = new Vector3(_pitch, _yaw, 0f);
        }
    }

    public override void _Process(double delta)
    {
        if (_suivi != null && !IsInstanceValid(_suivi))
            _suivi = null;   // robot supprimé
        if (_suivi != null)
        {
            // Basis.Z pointe derrière la caméra : on se place à DistanceSuivi derrière le robot, dans l'axe du regard
            Vector3 voulu = Centre(_suivi) + Basis.Z * DistanceSuivi;
            GlobalPosition = GlobalPosition.Lerp(voulu, 1f - Mathf.Exp(-8f * (float)delta));
            return;
        }

        if (Input.MouseMode != Input.MouseModeEnum.Captured)
            return;

        Vector2 entree = Input.GetVector("left", "right", "up", "down");
        Vector3 deplacement = Basis * new Vector3(entree.X, 0f, entree.Y);

        if (Input.IsPhysicalKeyPressed(Key.E)) deplacement += Vector3.Up;
        if (Input.IsPhysicalKeyPressed(Key.Q)) deplacement += Vector3.Down;

        float vitesse = Vitesse * (Input.IsKeyPressed(Key.Shift) ? MultiplicateurRapide : 1f);
        Position += deplacement.LimitLength(1f) * vitesse * (float)delta;
    }
}
