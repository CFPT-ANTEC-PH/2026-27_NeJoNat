using Godot;
using System.Collections.Generic;
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

    [Export] public float VitessePivot = 0.8f;       // m/s de chaque roue quand le robot cherche la ligne en pivotant
    [Export] public float RetourAllure = 0.5f;       // par seconde : vitesse à laquelle le robot réaccélère après un virage
    [Export] public float TempsTrou = 0.8f;          // s : ligne perdue bien centrée = trou dans le tracé, on continue tout droit

    private CapteurRoute[] _capteursLigne;
    private float _dernierEcart;    // où la ligne a été vue la dernière fois (+ gauche, - droite)
    private float _coteVirage;      // ligne vue en travers d'un côté (virage serré) : on pivotera de ce côté en la perdant
    private float _tempsApresTravers; // s de suivi normal depuis la ligne en travers : au-delà, ce n'était pas un virage
    private float _tempsPerdu;      // s depuis que plus aucun capteur ne voit la ligne
    private float _tempsSuivi;      // s de suivi sans interruption : distingue un vrai trou d'une ligne juste croisée en pivotant
    private bool _ligneVue;
    private float _allure = 1f;     // part de VitesseAuto : chute d'un coup en virage, remonte doucement en ligne droite
    private float _sensEvitement;   // garde le même sens tant que l'obstacle est là, sinon le robot hésite

    private void ReinitialiserAuto()
    {
        _dernierEcart = _coteVirage = _tempsApresTravers = _tempsPerdu = _tempsSuivi = _sensEvitement = 0f;
        _ligneVue = false;
        _allure = 1f;
    }

    private (float avance, float tourne) CommandeAuto() =>
        Capteurs == TypeCapteurs.Bas ? SuivreLigne() : Explorer();

    // Capteurs bas. Les capteurs voisins qui voient la ligne forment un groupe ; on suit le groupe
    // le plus proche de là où était la ligne, pour ne pas être attiré par une autre ligne qui croise.
    private (float, float) SuivreLigne()
    {
        _capteursLigne ??= SupportBas.GetChildren().OfType<CapteurRoute>().OrderBy(c => c.Position.X).ToArray();
        float largeur = _capteursLigne.Max(c => Mathf.Abs(c.Position.X));

        var groupes = new List<(float Centre, int Taille)>();
        for (int i = 0; i < _capteursLigne.Length; i++)
        {
            if (!_capteursLigne[i].SurLigne)
                continue;
            int debut = i;
            while (i + 1 < _capteursLigne.Length && _capteursLigne[i + 1].SurLigne)
                i++;
            float centre = (_capteursLigne[debut].Position.X + _capteursLigne[i].Position.X) / 2f / largeur;
            groupes.Add((centre, i - debut + 1));
        }

        if (groupes.Count == 0)
            return LignePerdue();

        _ligneVue = true;
        if (_tempsPerdu > 0.05f)
            _tempsSuivi = 0f;   // retrouvée après une vraie perte : nouveau suivi
        _tempsPerdu = 0f;
        _tempsSuivi += (float)GetPhysicsProcessDeltaTime();
        var g = groupes.MinBy(g => Mathf.Abs(g.Centre - _dernierEcart));

        // Ligne en travers (3 capteurs ou plus) : virage serré, T ou croisement. On ne sait pas encore lequel :
        // on retient de quel côté elle part et on avance tout droit, lentement. Si la ligne continue devant,
        // on la retrouve ; si c'était un virage, on la perd et on pivote du côté retenu.
        if (g.Taille >= 3)
        {
            if (Mathf.Abs(g.Centre) > 0.2f)
                _coteVirage = Mathf.Sign(g.Centre);
            _tempsApresTravers = 0f;
            return (VitesseAuto * 0.3f, 0f);
        }

        _tempsApresTravers += (float)GetPhysicsProcessDeltaTime();
        if (_tempsApresTravers > 0.3f)
            _coteVirage = 0f;   // la ligne continue vraiment : ce n'était pas un virage
        _dernierEcart = g.Centre;
        // Ralentit tout de suite quand la ligne part de côté, et ne réaccélère que progressivement :
        // dans une suite de virages, le robot reste prudent (il ne peut pas tourner serré à pleine vitesse)
        float dt = (float)GetPhysicsProcessDeltaTime();
        _allure = Mathf.Min(1f - 0.8f * Mathf.Abs(g.Centre), _allure + RetourAllure * dt);
        return (VitesseAuto * _allure, Mathf.Clamp(GainLigne * g.Centre, -1f, 1f));
    }

    private (float, float) LignePerdue()
    {
        _tempsPerdu += (float)GetPhysicsProcessDeltaTime();

        if (!_ligneVue)
            return (VitesseAuto * 0.5f, 0f);   // pas encore trouvée : avance doucement
        if (_coteVirage == 0f && _tempsSuivi > 0.3f && Mathf.Abs(_dernierEcart) < 0.3f && _tempsPerdu < TempsTrou)
            return (VitesseAuto * 0.6f, 0f);   // suivie bien centrée puis perdue : sans doute un trou, on continue

        // Pivote sur place, du côté du virage annoncé ou de là où on l'a vue en dernier.
        // Lentement : les capteurs sont loin devant l'essieu et balaient vite le sol.
        float cote = _coteVirage != 0f ? _coteVirage : _dernierEcart >= 0f ? 1f : -1f;
        return (0f, cote * VitessePivot / (MaxSpeed * TurnFactor));
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
