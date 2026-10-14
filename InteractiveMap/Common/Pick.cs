using Godot;

namespace RobotMaker.InteractiveMap;

public static class Pick
{
    // Point du monde sous la souris, ou null si le rayon ne touche rien
    public static Vector3? SousLaSouris(Node3D depuis, uint masque = 1)
    {
        Viewport vp = depuis.GetViewport();
        Camera3D cam = vp.GetCamera3D();
        Vector2 souris = vp.GetMousePosition();

        Vector3 origine = cam.ProjectRayOrigin(souris);
        Vector3 fin = origine + cam.ProjectRayNormal(souris) * 1000f;

        var requete = PhysicsRayQueryParameters3D.Create(origine, fin, masque);
        var resultat = depuis.GetWorld3D().DirectSpaceState.IntersectRay(requete);
        return resultat.Count > 0 ? (Vector3)resultat["position"] : null;
    }
}
