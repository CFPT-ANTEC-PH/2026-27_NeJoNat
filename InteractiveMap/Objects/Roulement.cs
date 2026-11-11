using Godot;

namespace RobotMaker.InteractiveMap;

// Résistance au roulement, que Godot ne simule pas : sans elle, une balle lancée roule sans fin.
// Couple constant opposé à la rotation, C × m × g × r (C ≈ 0,01-0,03 pour du caoutchouc sur une table).
// ponytail: appliqué aussi en l'air, négligeable pour des objets posés sur la table
public partial class Roulement : RigidBody3D
{
    [Export] public float Coefficient = 0.02f;   // C, sans unité
    [Export] public float Rayon = 0.5f;          // u, rayon sur lequel l'objet roule

    public override void _PhysicsProcess(double delta)
    {
        float couple = Coefficient * Mass * GetGravity().Length() * Rayon;
        Vector3 w = AngularVelocity;
        // Couple que la rotation peut absorber pendant ce pas : au-delà, l'objet s'arrêterait puis repartirait à l'envers
        float freinable = w.Length() / (float)delta / InverseInertiaMax();
        if (couple >= freinable)
            AngularVelocity = Vector3.Zero;
        else
            ApplyTorque(-w.Normalized() * couple);
    }

    private float InverseInertiaMax()
    {
        Vector3 i = PhysicsServer3D.BodyGetDirectState(GetRid()).InverseInertia;
        return Mathf.Max(i.X, Mathf.Max(i.Y, i.Z));
    }
}
