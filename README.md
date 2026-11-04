# 2026-27_NeJoNat

# Simulation 3D de Robots - Godot

Projet de simulation 3D réalisé par **Nathan**, **Joao** et **Nelson**.

## Description du projet

Le but de ce projet est de réaliser une simulation 3D sur Godot mettant en scène des robots autonomes.

Les robots évoluent sur une table de **3 mètres par 6 mètres**, bordée de rebords de **10 centimètres**. Chaque robot est équipé de divers capteurs lui permettant de se déplacer et de suivre un chemin précis.

### Structure d'un robot

- 2 roues arrière **motorisées**, assurant la propulsion et le changement de direction (conduite différentielle asservie en vitesse)
- 1 roue avant, centrée, non motorisée, au choix :
  - **roue folle** : pivote toute seule dans le sens du déplacement
  - **glisseur** : capsule qui glisse au sol
- 1 support capteurs, au choix :
  - **capteurs avant** : 3 capteurs d'obstacle (raycast), avec anti-collision qui ralentit et s'arrête avant l'obstacle
  - **capteurs bas** : 5 capteurs de ligne qui détectent la ligne noire dessinée dans la carte interactive. Le suivi gère les virages serrés et en épingle, les trous dans le tracé et les croisements (il garde la ligne qu'il suivait)

### Personnalisation

Les robots se personnalisent dans la scène **Constructor** (Main Menu → New Robot). L'aperçu du robot se met à jour en direct :

| Catégorie | Réglages |
|---|---|
| Body | couleur, longueur, largeur, masse |
| Wheels | couleur, taille des roues motrices, adhérence, type de roue avant |
| Motor | puissance (Lent / Normal / Rapide), vitesse de virage, pilotage (Automatique / Manuel) |
| Sensors | type (avant / bas), portée, distance d'arrêt |

Le bouton **Play** ouvre la carte interactive avec le robot configuré. Les robots y attendent **▶ Démarrer** pour qu'on puisse d'abord tracer la ligne et poser les obstacles ; **↺ Replacer** les remet à leur point de départ, et **Poser** ajoute un robot sauvegardé au choix, avec ses propres réglages (son nom s'affiche au-dessus). Quand on change la taille du robot, tout suit : roues (physique comprise), collisions, supports capteurs, roue avant.

## Commandes

| Action | Touche |
|---|---|
| Avancer / reculer | `up` / `down` |
| Tourner | `left` / `right` |
| Tourner la caméra | glisser clic gauche ou droit (constructeur) / clic droit maintenu (carte) |
| Bouger la caméra (carte) | clic droit maintenu + `ZQSD` / `WASD`, `E` / `Q` monter / descendre |
| Zoom | molette |
| Désélectionner l'outil (carte) | `Échap` |

## Organisation du projet

| Dossier | Contenu |
|---|---|
| `Scenes/` | robot (`robot.tscn`), constructeur, menu principal |
| `Scenes/Capteurs/` | capteur avant, capteur de ligne et les deux supports capteurs |
| `Scenes/FrontWheels/` | roue folle (`FlyWeel.tscn`) et glisseur (`Glisseur.tscn`) |
| `Scripts/` | `Robot.cs` (conduite, réglages, dimensions), `RobotAuto.cs` (pilotage automatique), constructeur, sauvegarde, caméra |
| `InteractiveMap/` | carte interactive : dessin de ligne au sol, ajout d'objets, caméra libre |
| `Assets/` | modèles 3D (Blender) |
| `Tests/` | tests automatiques (rotation, suivi de ligne, pose de robots, sauvegarde) |

### Échelle

**1 unité Godot = 10 cm** : la table fait 30 × 60 unités (3 m × 6 m) avec des rebords de 1 unité (10 cm), le robot environ 1,7 × 2,8 unités (17 × 28 cm).

### Tests

```
godot --headless res://Tests/TestRotation.tscn
godot --headless --fixed-fps 60 res://Tests/TestSuiviLigne.tscn   # 9 parcours, --fixed-fps les fait tourner en accéléré
godot --headless res://Tests/TestPoseRobots.tscn
godot --headless res://Tests/TestSauvegarde.tscn
```

Chaque test affiche `OK` ou `ECHEC` et quitte avec le code 0 ou 1.


## Outils utilisés

- **Godot** — moteur de jeu utilisé pour la simulation 3D et la programmation du comportement des robots
- **Blender** — modélisation 3D des robots

## État d'avancement

- [x] Prise en main de Godot et Blender
- [x] Modélisation 3D d'un premier robot sous Blender et import dans Godot
- [x] Création d'une carte de test
- [x] Script de déplacement de base pour un robot de test
- [x] Correction du bug : les roues traversent le sol
- [x] Roue folle stable et moteurs asservis
- [x] Capteurs avant d'obstacle et anti-collision
- [x] Carte interactive (dessin de ligne, objets, caméra libre)
- [x] Capteurs de ligne (détection)
- [x] Suivi de ligne automatique
- [x] Pilotage automatique dans la carte interactive (évitement d'obstacles ou suivi de ligne selon les capteurs)
- [x] Deux types de roue avant (roue folle / glisseur)
- [x] Constructeur : personnalisation du robot (apparence, dimensions, moteur, capteurs)
- [x] Sauvegarde / chargement des robots (automatique, fichiers `user://robots/*.cfg`)

## Bugs connus

| Bug | Statut | Sources |
|---|---|---|
| La gravité (9,8) n'est pas mise à l'échelle 1 unité = 10 cm : les chutes sont environ 3 fois plus lentes qu'en vrai | Ouvert : passer à 98 demande de réaccorder suspensions, forces et freinage | — |
| Les roues passent à travers le sol | Fixé | [Vidéo YouTube](https://www.youtube.com/watch?v=Wj1FfilAe2Y), [Doc VehicleBody3D](https://docs.godotengine.org/en/stable/classes/class_vehiclebody3d.html), [Asset Library](https://godotengine.org/asset-library/asset/1879) |

## Journal de bord

Le suivi détaillé de l'avancement du projet : [Journal de bord](https://docs.google.com/document/d/1H4nKOKKnprnX5YsHTaftCW5wJqea41p1ubrGYx5cAO8)