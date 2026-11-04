using Godot;
using System.Linq;

namespace RobotMaker.Scripts;

// Pilotage automatique : le robot décide seul avance / tourne à partir de ses capteurs.
// Mêmes conventions que le clavier : avance et tourne dans [-1, 1], tourne > 0 = à gauche (+X local).
public partial class Robot
{
    public enum TypePilotage { Manuel, Auto }

    [ExportGroup("Pilotage automatique")]
    [Export] public TypePilotage Pilotage = TypePilotage.Auto;
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float VitesseAuto = 0.4f;  // part de MaxSpeed
    [Export] public float GainLigne = 1.5f;          // force du virage par rapport à l'écart de la ligne
    [Export] public float DistanceEvitement = 2f;    // m : en dessous, le robot tourne pour éviter l'obstacle

    private CapteurRoute[] _capteursLigne;
    private float _dernierEcart;    // où la ligne a été vue la dernière fois (+ gauche, - droite)
    private float _sensEvitement;   // garde le même sens tant que l'obstacle est là, sinon le robot hésite

    private (float avance, float tourne) CommandeAuto() =>
        Capteurs == TypeCapteurs.Bas ? SuivreLigne() : Explorer();

    // Capteurs bas : écart moyen des capteurs sur la ligne, ramené dans [-1, 1]
    private (float, float) SuivreLigne()
    {
        _capteursLigne ??= SupportBas.GetChildren().OfType<CapteurRoute>().ToArray();
        float largeur = _capteursLigne.Max(c => Mathf.Abs(c.Position.X));
        float[] surLigne = _capteursLigne.Where(c => c.SurLigne).Select(c => c.Position.X / largeur).ToArray();

        // Ligne perdue : pivote du côté où on l'a vue en dernier ; jamais vue (ou perdue bien centrée) : avance doucement
        if (surLigne.Length == 0)
            return (_dernierEcart == 0f ? VitesseAuto * 0.5f : 0f, Mathf.Sign(_dernierEcart));

        _dernierEcart = surLigne.Average();
        return (VitesseAuto * (1f - 0.6f * Mathf.Abs(_dernierEcart)), Mathf.Clamp(GainLigne * _dernierEcart, -1f, 1f));
    }

    // Capteurs avant : tout droit, et tourne vers le côté le plus libre devant un obstacle
    private (float, float) Explorer()
    {
        float g = DistanceCapteur(CapteurGauche), c = DistanceCapteur(CapteurCentre), d = DistanceCapteur(CapteurDroite);

        if (Mathf.Min(c, Mathf.Min(g, d)) > DistanceEvitement)
        {
            _sensEvitement = 0f;
            return (VitesseAuto, 0f);
        }

        if (_sensEvitement == 0f)
            _sensEvitement = g > d ? 1f : g < d ? -1f : GD.Randf() < 0.5f ? 1f : -1f;

        // Obstacle juste devant : tourne sur place (l'anti-collision freine de toute façon)
        return (c < DistanceEvitement * 0.5f ? 0f : VitesseAuto * 0.5f, _sensEvitement);
    }
}
