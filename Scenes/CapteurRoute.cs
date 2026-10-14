using Godot;

public partial class CapteurSol : RayCast3D
{
    [Signal]
    public delegate void SurfaceChangeeEventHandler(string surface);

    [Export] public float Portee { get; set; } = 1.0f;
    [Export] public float Intervalle { get; set; } = 0.1f;

    public string Surface { get; private set; } = "inconnu";

    private double _temps;

    public override void _Ready()
    {
        Enabled = true;
        TargetPosition = new Vector3(0, -Portee, 0);

        //check que il se détéct pas lui meme
        if (GetParent() is CollisionObject3D corps)
            AddException(corps);
    }

    public override void _PhysicsProcess(double delta)
    {
        _temps += delta;
        if (_temps < Intervalle) return;
        _temps = 0;

        string lue = Lire();
        if (lue != Surface)
        {
            Surface = lue;
            EmitSignal(SignalName.SurfaceChangee, Surface);
        }
    }

    private string Lire()
    {
        if (IsColliding() && GetCollider() is Node sol && sol.HasMeta("surface"))
            return sol.GetMeta("surface").AsString();
        return "inconnu";
    }
}