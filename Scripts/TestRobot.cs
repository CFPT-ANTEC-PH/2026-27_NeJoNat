using Godot;

namespace RobotMaker.Scripts;

public partial class TestRobot : VehicleBody3D
{
    [ExportGroup("Roues")]
    [Export] public VehicleWheel3D LeftWheel;
    [Export] public VehicleWheel3D RightWheel;
    [Export] public VehicleWheel3D FrontWheel;   // roue folle : orientée automatiquement par le code

    [ExportGroup("Visuel roue libre (optionnel)")]
    [Export] public Node3D CasterPivot;          // fourche qui pivote
    [Export] public Node3D CasterMesh;           // petite roue, enfant du pivot
    [Export] public float CasterSwivelSpeed = 10f;

    [ExportGroup("Conduite")]
    [Export] public float MaxSpeed = 5f;         // m/s
    [Export] public float TurnFactor = 0.4f;     // part de MaxSpeed donnée à chaque roue pour tourner
    [Export] public float Acceleration = 4f;     // m/s² : montée progressive de la consigne
    [Export] public float Deceleration = 6f;     // m/s² au freinage (au-delà de ~9, risque de basculer en freinant en marche arrière)
    [Export] public float Force = 300f;          // force max d'un moteur (N)
    [Export] public float Gain = 800f;           // N par m/s d'écart entre consigne et vitesse réelle
    [Export] public float BrakeForce = 2f;       // frein de parking quand le robot est arrêté
    [Export] public float AccelLaterale = 4f;    // m/s² max en virage (au-delà, risque de se renverser)

    // NOUVEAU : capteurs
    [ExportGroup("Capteurs")]
    [Export] public CapteurAvant CapteurGauche;
    [Export] public CapteurAvant CapteurCentre;
    [Export] public CapteurAvant CapteurDroite;
    [Export] public bool AntiCollision = true;
    [Export] public float DistanceArret = 0.4f;  // m

    // NOUVEAU : infos utilisables partout dans le robot
    public bool ObstacleDevant => DistanceObstacle < float.PositiveInfinity;

    public float DistanceObstacle => Mathf.Min(DistanceCapteur(CapteurCentre),
        Mathf.Min(DistanceCapteur(CapteurGauche), DistanceCapteur(CapteurDroite)));

    private static float DistanceCapteur(CapteurAvant c) =>
        c != null && c.Detecte ? c.Distance : float.PositiveInfinity;

    private float _vitesseCible;   // m/s, avance
    private float _virageCible;    // m/s, différence ajoutée/retirée à chaque roue
    private Vector3 _casterOffset; // position du pivot par rapport au centre de la roue avant
    private float _voie;           // m, distance entre les deux roues motrices

    public override void _Ready()
    {
        if (LeftWheel == null || RightWheel == null || FrontWheel == null)
        {
            GD.PushError($"{Name} : LeftWheel, RightWheel et FrontWheel doivent être assignées dans l'inspecteur.");
            SetPhysicsProcess(false);
            return;
        }

        // Sécurité : on force la bonne config des roues
        LeftWheel.UseAsTraction = true;
        RightWheel.UseAsTraction = true;
        FrontWheel.UseAsTraction = false;

        // Pas de direction "voiture" : FrontWheel.Steering est piloté par UpdateCaster
        LeftWheel.UseAsSteering = false;
        RightWheel.UseAsSteering = false;
        FrontWheel.UseAsSteering = false;

        EngineForce = 0f;
        Steering = 0f;
        Brake = 0f;

        _voie = LeftWheel.Position.DistanceTo(RightWheel.Position);

        // Dans la scène, le pivot est placé pour une suspension au repos
        if (CasterPivot != null)
            _casterOffset = CasterPivot.Position - (FrontWheel.Position + Vector3.Down * FrontWheel.WheelRestLength);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        float avance = Input.GetAxis("down", "up");
        float tourne = Input.GetAxis("right", "left");

        // NOUVEAU : ralentit à l'approche d'un obstacle pour pouvoir s'arrêter à DistanceArret
        // (v² = 2 × décélération × distance restante ; reculer et tourner restent possibles).
        // On planifie avec la moitié de Deceleration pour garder de la marge (retard des moteurs).
        if (AntiCollision && avance > 0f)
        {
            float vitesseSure = Mathf.Sqrt(Deceleration * Mathf.Max(DistanceObstacle - DistanceArret, 0f));
            avance = Mathf.Min(avance, vitesseSure / MaxSpeed);
        }

        // Consigne lissée, en m/s (on freine plus fort qu'on accélère)
        float cible = avance * MaxSpeed;
        bool ralentit = Mathf.Abs(cible) < Mathf.Abs(_vitesseCible) || cible * _vitesseCible < 0f;
        _vitesseCible = Mathf.MoveToward(_vitesseCible, cible, (ralentit ? Deceleration : Acceleration) * dt);

        // Anti-renversement : en virage, accélération latérale = vitesse × vitesse de rotation,
        // et vitesse de rotation = 2 × virage / voie. On réduit le virage quand on va vite.
        float virageMax = MaxSpeed * TurnFactor;
        if (Mathf.Abs(_vitesseCible) > 0.01f)
            virageMax = Mathf.Min(virageMax, AccelLaterale * _voie / (2f * Mathf.Abs(_vitesseCible)));
        _virageCible = Mathf.MoveToward(_virageCible, tourne * virageMax, Acceleration * dt);

        // Conduite différentielle asservie en vitesse (comme un moteur avec encodeur) :
        // le robot freine tout seul quand on lâche, et tourne sur place sans dériver
        LeftWheel.EngineForce = ForceMoteur(LeftWheel, _vitesseCible - _virageCible);
        RightWheel.EngineForce = ForceMoteur(RightWheel, _vitesseCible + _virageCible);

        // Frein de parking seulement une fois arrêté, sinon il gênerait l'asservissement
        bool arrete = avance == 0f && tourne == 0f && LinearVelocity.Length() < 0.1f;
        LeftWheel.Brake = arrete ? BrakeForce : 0f;
        RightWheel.Brake = arrete ? BrakeForce : 0f;

        UpdateCaster(dt);
    }

    private float ForceMoteur(VehicleWheel3D roue, float vitesseVoulue)
    {
        float vitesseRoue = VitesseAuPoint(roue.GlobalPosition).Dot(GlobalBasis.Z);
        return Mathf.Clamp((vitesseVoulue - vitesseRoue) * Gain, -Force, Force);
    }

    // Vitesse d'un point du robot (translation + rotation autour du centre de masse)
    private Vector3 VitesseAuPoint(Vector3 pointGlobal) =>
        PhysicsServer3D.BodyGetDirectState(GetRid()).GetVelocityAtLocalPosition(pointGlobal - GlobalPosition);

    private void UpdateCaster(float dt)
    {
        Vector3 vitesseMonde = VitesseAuPoint(FrontWheel.GlobalPosition);
        Vector3 vitesseLocale = GlobalBasis.Inverse() * vitesseMonde;
        vitesseLocale.Y = 0f;

        float vitesse = vitesseLocale.Length();

        if (vitesse > 0.05f)
        {
            // Roue folle physique : on l'aligne sur son déplacement pour qu'elle
            // n'oppose aucune force latérale (c'est ce qui faisait basculer le robot).
            // Une roue tournée de 180° a le même axe, donc on reste dans [-90°, 90°].
            float steering = Mathf.Atan2(vitesseLocale.X, vitesseLocale.Z);
            if (steering > Mathf.Pi / 2f)
                steering -= Mathf.Pi;
            else if (steering < -Mathf.Pi / 2f)
                steering += Mathf.Pi;
            FrontWheel.Steering = steering;
        }

        if (CasterPivot == null)
            return;

        // Le pivot suit la suspension pour que la roue visuelle touche le sol
        CasterPivot.Position = FrontWheel.Position + _casterOffset;

        if (vitesse > 0.1f)
        {
            // -90° car la roue a son axe sur Z (elle avance sur son X local)
            float angleCible = Mathf.Atan2(vitesseLocale.X, vitesseLocale.Z) - Mathf.Pi / 2f;
            Vector3 rot = CasterPivot.Rotation;
            rot.Y = Mathf.LerpAngle(rot.Y, angleCible, CasterSwivelSpeed * dt);
            CasterPivot.Rotation = rot;
        }

        // Vitesse signée le long du sens de roulement actuel de la roue (X local du pivot) :
        // tourne à l'envers en reculant, tant que la fourche n'a pas fini de pivoter
        float roulement = vitesseMonde.Dot(CasterPivot.GlobalBasis.X.Normalized());

        if (CasterMesh != null)
            CasterMesh.RotateObjectLocal(Vector3.Forward, roulement / FrontWheel.WheelRadius * dt);
    }
}
