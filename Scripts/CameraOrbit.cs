using Godot;

namespace RobotMaker.Scripts;

public partial class CameraOrbit : Node3D
{
    [Export] public Node3D Target;

    [ExportGroup("Souris")]
    [Export] public float Sensibilite = 0.003f;
    [Export] public float PitchMin = -70f;   // regarder vers le bas (degrés)
    [Export] public float PitchMax = 20f;    // regarder vers le haut (degrés)

    [ExportGroup("Distance")]
    [Export] public float Distance = 4f;
    [Export] public float DistanceMin = 1.5f;
    [Export] public float DistanceMax = 10f;

    [ExportGroup("Suivi")]
    [Export] public float Hauteur = 1f;      // point visé au-dessus du robot
    [Export] public float SuiviDouceur = 10f;

    private SpringArm3D _arm;
    private float _yaw;
    private float _pitch;

    public override void _Ready()
    {
        _arm = GetNode<SpringArm3D>("SpringArm3D");
        _arm.SpringLength = Distance;

        // Ignore le transform du parent, même si le pivot est placé sous le robot
        TopLevel = true;

        // La caméra ne se cogne pas contre le robot lui-même
        if (Target is CollisionObject3D corps)
            _arm.AddExcludedObject(corps.GetRid());

        _pitch = Mathf.DegToRad(-20f);
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        // Rotation avec la souris
        if (e is InputEventMouseMotion souris && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _yaw -= souris.Relative.X * Sensibilite;
            _pitch -= souris.Relative.Y * Sensibilite;
            _pitch = Mathf.Clamp(_pitch, Mathf.DegToRad(PitchMin), Mathf.DegToRad(PitchMax));
        }
        else if (e is InputEventMouseButton bouton && bouton.Pressed)
        {
            // Zoom avec la molette
            if (bouton.ButtonIndex == MouseButton.WheelUp)
                Distance -= 0.5f;
            else if (bouton.ButtonIndex == MouseButton.WheelDown)
                Distance += 0.5f;
            // Clic gauche pour recapturer la souris
            else if (bouton.ButtonIndex == MouseButton.Left)
                Input.MouseMode = Input.MouseModeEnum.Captured;

            Distance = Mathf.Clamp(Distance, DistanceMin, DistanceMax);
        }
        // Échap pour libérer la souris
        else if (e.IsActionPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public override void _Process(double delta)
    {
        if (Target == null)
            return;

        float dt = (float)delta;

        // Suit le robot en douceur
        Vector3 cible = Target.GlobalPosition + Vector3.Up * Hauteur;
        GlobalPosition = GlobalPosition.Lerp(cible, SuiviDouceur * dt);

        // Applique la rotation de la souris
        Rotation = new Vector3(_pitch, _yaw, 0f);

        // Zoom progressif
        _arm.SpringLength = Mathf.Lerp(_arm.SpringLength, Distance, 10f * dt);
    }
}