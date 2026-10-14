using Godot;

namespace RobotMaker.InteractiveMap;

// Caméra libre : clic droit maintenu pour regarder, WASD/ZQSD pour bouger,
// E/Q monter/descendre, Shift pour aller vite, molette pour régler la vitesse
public partial class FreeCamera : Camera3D
{
    [Export] public float Vitesse = 10f;
    [Export] public float MultiplicateurRapide = 3f;
    [Export] public float Sensibilite = 0.003f;

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
            else if (bouton.Pressed && bouton.ButtonIndex == MouseButton.WheelUp)
                Vitesse *= 1.1f;
            else if (bouton.Pressed && bouton.ButtonIndex == MouseButton.WheelDown)
                Vitesse /= 1.1f;
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
        Vector2 entree = Input.GetVector("left", "right", "up", "down");
        Vector3 deplacement = Basis * new Vector3(entree.X, 0f, entree.Y);

        if (Input.IsKeyPressed(Key.E)) deplacement += Vector3.Up;
        if (Input.IsKeyPressed(Key.Q)) deplacement += Vector3.Down;

        float vitesse = Vitesse * (Input.IsKeyPressed(Key.Shift) ? MultiplicateurRapide : 1f);
        Position += deplacement.LimitLength(1f) * vitesse * (float)delta;
    }
}
