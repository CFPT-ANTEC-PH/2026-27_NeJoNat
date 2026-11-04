using Godot;

namespace RobotMaker.InteractiveMap;

public static class Pick
{
    // Point du monde sous la souris, ou null si le rayon ne touche rien.
    // ecran : position de l'événement souris (sinon position actuelle de la souris)
    public static Vector3? SousLaSouris(Node3D depuis, uint masque = 1, Vector2? ecran = null) =>
        Rayon(depuis, masque, ecran) is { Count: > 0 } r ? (Vector3)r["position"] : null;

    // Objet (corps physique) sous la souris, ou null
    public static GodotObject ObjetSousLaSouris(Node3D depuis, uint masque = 1, Vector2? ecran = null) =>
        Rayon(depuis, masque, ecran) is { Count: > 0 } r ? (GodotObject)r["collider"] : null;

    private static Godot.Collections.Dictionary Rayon(Node3D depuis, uint masque, Vector2? ecran)
    {
        Viewport vp = depuis.GetViewport();
        Camera3D cam = vp.GetCamera3D();
        Vector2 souris = ecran ?? vp.GetMousePosition();

        Vector3 origine = cam.ProjectRayOrigin(souris);
        Vector3 fin = origine + cam.ProjectRayNormal(souris) * 1000f;

        var requete = PhysicsRayQueryParameters3D.Create(origine, fin, masque);
        return depuis.GetWorld3D().DirectSpaceState.IntersectRay(requete);
    }
}
