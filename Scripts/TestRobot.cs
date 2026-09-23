using Godot;

namespace RobotMaker.Scripts;

public partial class TestRobot : VehicleBody3D
{
    [ExportGroup("Roues")]
    [Export] public VehicleWheel3D LeftWheel;
    [Export] public VehicleWheel3D RightWheel;
    [Export] public VehicleWheel3D FrontWheel;

    [ExportGroup("Visuel roue libre (optionnel)")]
    [Export] public Node3D CasterPivot;          // fourche qui pivote
    [Export] public Node3D CasterMesh;           // petite roue, enfant du pivot
    [Export] public float CasterSwivelSpeed = 10f;

    [ExportGroup("Conduite")]
    [Export] public float Force = 50f;
    [Export] public float TurnFactor = 0.7f;     // < 1 = virages plus doux
    [Export] public float Acceleration = 150f;   // vitesse de montée de la force
    [Export] public float MaxSpeed = 5f;         // m/s
    [Export] public float BrakeForce = 2f;

    private float _leftForce;
    private float _rightForce;

    public override void _Ready()
    {
        // Sécurité : on force la bonne config des roues
        LeftWheel.UseAsTraction = true;
        RightWheel.UseAsTraction = true;
        FrontWheel.UseAsTraction = false;

        LeftWheel.UseAsSteering = false;
        RightWheel.UseAsSteering = false;
        FrontWheel.UseAsSteering = false;

        EngineForce = 0f;
        Steering = 0f;
        Brake = 0f;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        float avance = Input.GetAxis("down", "up");
        float tourne = Input.GetAxis("right", "left") * TurnFactor;
        bool aucunInput = avance == 0f && tourne == 0f;

        // Limite de vitesse : on coupe la poussée si on va déjà trop vite
        float vitesse = LinearVelocity.Dot(GlobalBasis.Z);
        if (Mathf.Abs(vitesse) > MaxSpeed && Mathf.Sign(avance) == Mathf.Sign(vitesse))
            avance = 0f;

        // Conduite différentielle
        float cibleG = Mathf.Clamp(avance - tourne, -1f, 1f) * Force;
        float cibleD = Mathf.Clamp(avance + tourne, -1f, 1f) * Force;

        // Montée progressive de la force
        _leftForce = Mathf.MoveToward(_leftForce, cibleG, Acceleration * dt);
        _rightForce = Mathf.MoveToward(_rightForce, cibleD, Acceleration * dt);

        LeftWheel.EngineForce = _leftForce;
        RightWheel.EngineForce = _rightForce;

        // Frein uniquement sur les roues arrière
        float frein = aucunInput ? BrakeForce : 0f;
        LeftWheel.Brake = frein;
        RightWheel.Brake = frein;
        FrontWheel.Brake = 0f;

        UpdateCaster(dt);
    }

    private void UpdateCaster(float dt)
    {
        if (CasterPivot == null)
            return;

        CasterPivot.GlobalPosition = FrontWheel.GlobalPosition;

        Vector3 offset = FrontWheel.GlobalPosition - GlobalPosition;
        Vector3 vitesseMonde = LinearVelocity + AngularVelocity.Cross(offset);
        Vector3 vitesseLocale = GlobalBasis.Inverse() * vitesseMonde;
        vitesseLocale.Y = 0f;

        float vitesse = vitesseLocale.Length();

        if (vitesse > 0.1f)
        {
            // -90° car la roue a son axe sur Z (elle avance sur son X local)
            float angleCible = Mathf.Atan2(vitesseLocale.X, vitesseLocale.Z) - Mathf.Pi / 2f;
            Vector3 rot = CasterPivot.Rotation;
            rot.Y = Mathf.LerpAngle(rot.Y, angleCible, CasterSwivelSpeed * dt);
            CasterPivot.Rotation = rot;
        }

        if (CasterMesh != null)
            CasterMesh.RotateObjectLocal(Vector3.Forward, vitesse / FrontWheel.WheelRadius * dt);
    }
}