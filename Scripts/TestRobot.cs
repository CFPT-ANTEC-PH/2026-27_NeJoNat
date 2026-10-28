using Godot;
using System.Collections.Generic;
using System.Linq;

namespace RobotMaker.Scripts;

public partial class TestRobot : VehicleBody3D
{
    [ExportGroup("Roues")]
    [Export] public VehicleWheel3D LeftWheel;
    [Export] public VehicleWheel3D RightWheel;
    [Export] public VehicleWheel3D FrontWheel;   // roue folle : orientée automatiquement par le code

    // Roue avant : roue folle qui pivote, ou capsule qui glisse au sol
    public enum TypeRoueAvant { RoueFolle, Glisseur }

    [ExportGroup("Roue avant")]
    [Export] public TypeRoueAvant RoueAvant = TypeRoueAvant.RoueFolle;
    [Export] public Node3D RoueFolle;
    [Export] public Node3D Glisseur;
    [Export] public float AdherenceGlisseur = 0f; // wheel_friction_slip du glisseur : 0 = glisse sans aucune accroche

    [ExportGroup("Visuel roue libre (optionnel)")]
    [Export] public Node3D CasterPivot;          // fourche qui pivote
    [Export] public Node3D CasterMesh;           // petite roue, enfant du pivot
    [Export] public float CasterSwivelSpeed = 10f;

    // Dimensions : le châssis s'allonge vers l'avant (l'arrière ne bouge pas) et s'élargit des deux côtés.
    // Tout l'avant (roue avant, supports capteurs, pare-chocs) suit le bout du châssis,
    // les roues motrices gardent leur distance au bord du châssis.
    [ExportGroup("Dimensions")]
    [Export(PropertyHint.Range, "0.7,1.6,0.05")] public float EchelleLongueur = 1f;  // 1 = taille d'origine
    [Export(PropertyHint.Range, "0.7,1.6,0.05")] public float EchelleLargeur = 1f;
    [Export(PropertyHint.Range, "0.7,1.4,0.05")] public float EchelleRoues = 1f;     // roues motrices : le châssis garde sa hauteur
    [Export] public Node3D ChassisMesh;
    [Export] public CollisionShape3D ChassisCollision;   // doit être une BoxShape3D : sert de référence de taille
    [Export] public CollisionShape3D AvantCollision;

    [ExportGroup("Apparence")]
    [Export] public Color CouleurChassis = new(0.62f, 0.81f, 0.93f);
    [Export] public Color CouleurRoues = Colors.Transparent;   // transparent = couleur d'origine du modèle

    [ExportGroup("Adhérence")]
    [Export(PropertyHint.Range, "0.2,2,0.05")] public float EchelleAdherence = 1f;  // multiplie l'adhérence des roues motrices de la scène

    // Moteur : multiplie MaxSpeed et Force réglés dans l'inspecteur
    public enum TypeMoteur { Lent, Normal, Rapide }

    [ExportGroup("Conduite")]
    [Export] public TypeMoteur Moteur = TypeMoteur.Normal;
    [Export] public float MaxSpeed = 5f;         // m/s
    [Export] public float TurnFactor = 0.4f;     // part de MaxSpeed donnée à chaque roue pour tourner
    [Export] public float Acceleration = 4f;     // m/s² : montée progressive de la consigne
    [Export] public float Deceleration = 6f;     // m/s² au freinage (au-delà de ~9, risque de basculer en freinant en marche arrière)
    [Export] public float Force = 300f;          // force max d'un moteur (N)
    [Export] public float Gain = 800f;           // N par m/s d'écart entre consigne et vitesse réelle
    [Export] public float BrakeForce = 2f;       // frein de parking quand le robot est arrêté
    [Export] public float AccelLaterale = 4f;    // m/s² max en virage (au-delà, risque de se renverser)

    // NOUVEAU : capteurs
    public enum TypeCapteurs { Avant, Bas }

    [ExportGroup("Capteurs")]
    [Export] public TypeCapteurs Capteurs = TypeCapteurs.Avant;
    [Export] public Node3D SupportAvant;
    [Export] public Node3D SupportBas;
    [Export] public CapteurAvant CapteurGauche;
    [Export] public CapteurAvant CapteurCentre;
    [Export] public CapteurAvant CapteurDroite;
    [Export] public bool AntiCollision = true;
    [Export] public float DistanceArret = 0.4f;  // m
    [Export(PropertyHint.Range, "0.5,8,0.25")] public float PorteeCapteurs = 3f;  // m, appliquée aux 3 capteurs avant

    // NOUVEAU : infos utilisables partout dans le robot
    public bool ObstacleDevant => DistanceObstacle < float.PositiveInfinity;

    public float DistanceObstacle => Mathf.Min(DistanceCapteur(CapteurCentre),
        Mathf.Min(DistanceCapteur(CapteurGauche), DistanceCapteur(CapteurDroite)));

    private static float DistanceCapteur(CapteurAvant c) =>
        c != null && c.Detecte ? c.Distance : float.PositiveInfinity;

    // Réglages modifiables dans le constructeur. Sauver() les copie en quittant le constructeur,
    // et le robot de la scène de jeu les reprend à la place de ceux de l'inspecteur.
    public static readonly string[] Reglages =
    {
        nameof(RoueAvant), nameof(Capteurs), nameof(Moteur), nameof(CouleurChassis), nameof(CouleurRoues),
        nameof(EchelleLongueur), nameof(EchelleLargeur), nameof(EchelleRoues), nameof(EchelleAdherence),
        nameof(PorteeCapteurs), nameof(TurnFactor), nameof(DistanceArret), "mass",
    };

    public static Dictionary<string, Variant> Choix;

    public void Sauver() => Choix = Reglages.ToDictionary(nom => nom, nom => Get(nom));

    private float _adherenceRoueFolle, _adherenceRoues;

    // Valeurs de la scène, pour recalculer les dimensions depuis l'origine à chaque changement
    private readonly Dictionary<Node3D, Transform3D> _origines = new();
    private BoxShape3D _boxChassis, _boxAvant;
    private Vector3 _tailleChassis, _tailleAvant, _centreMasse;
    private float _rayonRoues;
    private float _vitesseBase, _forceBase;

    private float _vitesseCible;   // m/s, avance
    private float _virageCible;    // m/s, différence ajoutée/retirée à chaque roue
    private Vector3 _casterOffset; // position du pivot par rapport au centre de la roue avant

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

        if (Choix != null)
            foreach ((string nom, Variant valeur) in Choix)
                Set(nom, valeur);

        _adherenceRoueFolle = FrontWheel.WheelFrictionSlip;
        _adherenceRoues = LeftWheel.WheelFrictionSlip;
        _vitesseBase = MaxSpeed;
        _forceBase = Force;
        PreparerDimensions();
        Appliquer();

        // Dans la scène, le pivot est placé pour une suspension au repos
        if (CasterPivot != null)
            _casterOffset = ToLocal(CasterPivot.GlobalPosition) - (FrontWheel.Position + Vector3.Down * FrontWheel.WheelRestLength);
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
            virageMax = Mathf.Min(virageMax, AccelLaterale * Voie / (2f * Mathf.Abs(_vitesseCible)));
        _virageCible = Mathf.MoveToward(_virageCible, tourne * virageMax, Acceleration * dt);

        // Conduite différentielle asservie en vitesse (comme un moteur avec encodeur) :
        // le robot freine tout seul quand on lâche, et tourne sur place sans dériver
        LeftWheel.EngineForce = ForceMoteur(LeftWheel, _vitesseCible - _virageCible);
        RightWheel.EngineForce = ForceMoteur(RightWheel, _vitesseCible + _virageCible);

        // Frein de parking seulement une fois arrêté, sinon il gênerait l'asservissement
        bool arrete = avance == 0f && tourne == 0f && LinearVelocity.Length() < 0.1f;
        LeftWheel.Brake = arrete ? BrakeForce : 0f;
        RightWheel.Brake = arrete ? BrakeForce : 0f;

        if (RoueAvant == TypeRoueAvant.RoueFolle)
            UpdateCaster(dt);
    }

    // Rappelée par le constructeur à chaque changement de réglage
    public void Appliquer()
    {
        AppliquerChoix();
        AppliquerDimensions();
    }

    private void AppliquerChoix()
    {
        Activer(SupportAvant, Capteurs == TypeCapteurs.Avant);
        Activer(SupportBas, Capteurs == TypeCapteurs.Bas);

        // Le glisseur garde la suspension de FrontWheel (il porte le robot) mais n'accroche pas le sol
        Activer(RoueFolle, RoueAvant == TypeRoueAvant.RoueFolle);
        Activer(Glisseur, RoueAvant == TypeRoueAvant.Glisseur);
        FrontWheel.WheelFrictionSlip = RoueAvant == TypeRoueAvant.Glisseur ? AdherenceGlisseur : _adherenceRoueFolle;

        float puissance = Moteur switch { TypeMoteur.Lent => 0.6f, TypeMoteur.Rapide => 1.6f, _ => 1f };
        MaxSpeed = _vitesseBase * puissance;
        Force = _forceBase * puissance;

        LeftWheel.WheelFrictionSlip = _adherenceRoues * EchelleAdherence;
        RightWheel.WheelFrictionSlip = _adherenceRoues * EchelleAdherence;

        foreach (CapteurAvant capteur in new[] { CapteurGauche, CapteurCentre, CapteurDroite })
            if (capteur != null)
                capteur.Portee = PorteeCapteurs;

        if (ChassisMesh is GeometryInstance3D chassis)
            chassis.MaterialOverride = new StandardMaterial3D { AlbedoColor = CouleurChassis };

        // Tous les modèles 3D des roues motrices ; transparent = on remet leur matériau d'origine
        var matRoues = CouleurRoues.A > 0f ? new StandardMaterial3D { AlbedoColor = CouleurRoues } : null;
        foreach (VehicleWheel3D roue in new[] { LeftWheel, RightWheel })
            foreach (Node n in roue.FindChildren("*", nameof(MeshInstance3D), true, false))
                ((MeshInstance3D)n).MaterialOverride = matRoues;
    }

    private void PreparerDimensions()
    {
        if (ChassisCollision?.Shape is not BoxShape3D)
        {
            GD.PushWarning($"{Name} : ChassisCollision doit avoir une BoxShape3D pour pouvoir changer les dimensions.");
            return;
        }

        foreach (Node3D n in new Node3D[] { LeftWheel, RightWheel, FrontWheel, RoueFolle, Glisseur, SupportAvant, SupportBas, ChassisMesh, ChassisCollision, AvantCollision })
            if (n != null)
                _origines[n] = n.Transform;

        // Contenu des supports capteurs (le modèle s'élargit, les capteurs s'écartent) et visuel des roues motrices
        _rayonRoues = LeftWheel.WheelRadius;
        foreach (Node3D support in new[] { SupportAvant, SupportBas, LeftWheel, RightWheel })
            if (support != null)
                foreach (Node enfant in support.GetChildren())
                    if (enfant is Node3D n)
                        _origines[n] = n.Transform;

        // Formes propres à ce robot, sinon tous les robots de la partie changeraient de taille ensemble
        _boxChassis = (BoxShape3D)ChassisCollision.Shape.Duplicate();
        ChassisCollision.Shape = _boxChassis;
        _tailleChassis = _boxChassis.Size;

        if (AvantCollision?.Shape is BoxShape3D avant)
        {
            _boxAvant = (BoxShape3D)avant.Duplicate();
            AvantCollision.Shape = _boxAvant;
            _tailleAvant = _boxAvant.Size;
        }

        _centreMasse = CenterOfMass;
    }

    private void AppliquerDimensions()
    {
        if (_boxChassis == null)
            return;

        float allonge = _tailleChassis.Z * (EchelleLongueur - 1f);
        float elargit = _tailleChassis.X * (EchelleLargeur - 1f);

        // Châssis étiré vers l'avant : son centre avance de la moitié de l'allongement
        _boxChassis.Size = _tailleChassis + new Vector3(elargit, 0f, allonge);
        Deplacer(ChassisCollision, new Vector3(0f, 0f, allonge / 2f));

        if (ChassisMesh != null)
        {
            float centreZ = _origines[ChassisCollision].Origin.Z;
            Transform3D t = _origines[ChassisMesh];
            t.Basis = Basis.FromScale(new Vector3(EchelleLargeur, 1f, EchelleLongueur)) * t.Basis;
            t.Origin.X *= EchelleLargeur;
            t.Origin.Z = centreZ + allonge / 2f + (t.Origin.Z - centreZ) * EchelleLongueur;
            ChassisMesh.Transform = t;
        }

        // L'avant suit le bout du châssis
        if (_boxAvant != null)
            _boxAvant.Size = _tailleAvant + new Vector3(elargit, 0f, 0f);
        foreach (Node3D n in new Node3D[] { AvantCollision, SupportAvant, SupportBas, RoueFolle, Glisseur, FrontWheel })
            Deplacer(n, new Vector3(0f, 0f, allonge));

        // Supports capteurs : le modèle est étiré en largeur, les capteurs gardent leur taille mais s'écartent
        foreach (Node3D support in new[] { SupportAvant, SupportBas })
        {
            if (support == null)
                continue;
            foreach (Node enfant in support.GetChildren())
            {
                if (enfant is not Node3D n || !_origines.TryGetValue(n, out Transform3D t))
                    continue;
                if (n is MeshInstance3D)
                    t.Basis = Basis.FromScale(new Vector3(EchelleLargeur, 1f, 1f)) * t.Basis;
                t.Origin.X *= EchelleLargeur;
                n.Transform = t;
            }
        }

        // Roues motrices : chacune s'écarte de la moitié de l'élargissement. Une roue plus grande
        // est fixée plus haut d'autant, pour que le châssis reste à la même hauteur du sol.
        float rayon = _rayonRoues * EchelleRoues;
        foreach (VehicleWheel3D roue in new[] { LeftWheel, RightWheel })
        {
            roue.WheelRadius = rayon;
            Deplacer(roue, new Vector3(Mathf.Sign(_origines[roue].Origin.X) * elargit / 2f, rayon - _rayonRoues, 0f));
            foreach (Node enfant in roue.GetChildren())
            {
                if (enfant is not Node3D n || !_origines.TryGetValue(n, out Transform3D t))
                    continue;
                t.Basis = Basis.FromScale(Vector3.One * EchelleRoues) * t.Basis;
                n.Transform = t;
            }
        }

        CenterOfMass = _centreMasse + new Vector3(0f, 0f, allonge / 2f);

        // Une VehicleWheel3D ne lit sa position qu'en entrant dans l'arbre : on la ressort et on la remet
        foreach (VehicleWheel3D roue in new[] { LeftWheel, RightWheel, FrontWheel })
        {
            RemoveChild(roue);
            AddChild(roue);
        }
    }

    private void Deplacer(Node3D noeud, Vector3 decalage)
    {
        if (noeud == null)
            return;
        Transform3D t = _origines[noeud];
        t.Origin += decalage;
        noeud.Transform = t;
    }

    // L'élément non choisi est caché et arrêté (les capteurs gardent Detecte à false)
    private static void Activer(Node3D noeud, bool actif)
    {
        if (noeud == null)
            return;
        noeud.Visible = actif;
        noeud.ProcessMode = actif ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
    }

    // m, distance entre les deux roues motrices (change avec la largeur)
    private float Voie => LeftWheel.Position.DistanceTo(RightWheel.Position);

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
        CasterPivot.GlobalPosition = ToGlobal(FrontWheel.Position + _casterOffset);

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
