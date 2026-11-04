using Godot;
using System.Collections.Generic;
using System.Linq;

namespace RobotMaker.Scripts;

// Un robot = un fichier user://robots/<nom>.cfg avec ses réglages (Robot.Choix).
// ConfigFile garde les types Godot (Color, enums...) sans conversion.
public static class Sauvegarde
{
    private const string Dossier = "user://robots/";
    private const string Section = "robot";

    // Caractères interdits dans un nom de fichier remplacés par _ (même liste que Godot)
    public static string Fichier(string nom) =>
        Dossier + string.Concat(nom.Select(c => ":/\\?*\"|%<>".Contains(c) ? '_' : c)) + ".cfg";

    public static bool Existe(string nom) => FileAccess.FileExists(Fichier(nom));

    public static void Ecrire(Dictionary<string, Variant> choix)
    {
        DirAccess.MakeDirRecursiveAbsolute(Dossier);
        var cfg = new ConfigFile();
        foreach ((string cle, Variant valeur) in choix)
            cfg.SetValue(Section, cle, valeur);

        string nom = choix[nameof(Robot.NomRobot)].AsString();
        Error err = cfg.Save(Fichier(nom));
        if (err != Error.Ok)
            GD.PushError($"Sauvegarde du robot « {nom} » impossible : {err}");
    }

    public static Dictionary<string, Variant> Lire(string fichier)
    {
        var cfg = new ConfigFile();
        if (cfg.Load(fichier) != Error.Ok)
            return null;
        return cfg.GetSectionKeys(Section).ToDictionary(cle => cle, cle => cfg.GetValue(Section, cle));
    }

    // (fichier, nom affiché), triés par nom
    public static List<(string Fichier, string Nom)> Liste()
    {
        if (!DirAccess.DirExistsAbsolute(Dossier))
            return new();

        var robots = new List<(string, string)>();
        foreach (string f in DirAccess.GetFilesAt(Dossier).Where(f => f.EndsWith(".cfg")))
        {
            var cfg = new ConfigFile();
            if (cfg.Load(Dossier + f) == Error.Ok)
                robots.Add((Dossier + f, cfg.GetValue(Section, nameof(Robot.NomRobot), f.GetBaseName()).AsString()));
        }
        return robots.OrderBy(r => r.Item2).ToList();
    }

    public static void Supprimer(string fichier) => DirAccess.RemoveAbsolute(fichier);
}
