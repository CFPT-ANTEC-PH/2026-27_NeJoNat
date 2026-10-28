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
  - **capteurs bas** : 5 capteurs de ligne qui détectent la ligne noire dessinée dans la carte interactive

### Personnalisation

Les robots se personnalisent dans la scène **Constructor** (Main Menu → New Robot). L'aperçu du robot se met à jour en direct :

| Catégorie | Réglages |
|---|---|
| Body | couleur, longueur, largeur, masse |
| Wheels | couleur, taille des roues motrices, adhérence, type de roue avant |
| Motor | puissance (Lent / Normal / Rapide), vitesse de virage |
| Sensors | type (avant / bas), portée, distance d'arrêt |

Le bouton **Play** lance la scène de jeu avec le robot configuré. Quand on change la taille du robot, tout suit : roues (physique comprise), collisions, supports capteurs, roue avant.

## Commandes

| Action | Touche |
|---|---|
| Avancer / reculer | `up` / `down` |
| Tourner | `left` / `right` |
| Tourner la caméra | souris (en jeu) / glisser clic gauche ou droit (constructeur) |
| Zoom | molette |
| Libérer la souris | `Échap` |

## Organisation du projet

| Dossier | Contenu |
|---|---|
| `Scenes/` | robot (`robot.tscn`), monde de test, constructeur, menu principal |
| `Scenes/Cappteurs/` | capteur avant, capteur de ligne et les deux supports capteurs |
| `Scenes/FrontWeels/` | roue folle (`FlyWeel.tscn`) et glisseur (`Glisseur.tscn`) |
| `Scripts/` | `TestRobot.cs` (conduite, réglages, dimensions), constructeur, caméra |
| `InteractiveMap/` | carte interactive : dessin de ligne au sol, ajout d'objets, caméra libre |
| `Assets/` | modèles 3D (Blender) |


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
- [ ] Suivi de ligne automatique
- [x] Deux types de roue avant (roue folle / glisseur)
- [x] Constructeur : personnalisation du robot (apparence, dimensions, moteur, capteurs)
- [ ] Sauvegarde / chargement d'un robot (Load Robot)

## Bugs connus

| Bug | Statut | Sources |
|---|---|---|
| Les roues passent à travers le sol | Fixé | [Vidéo YouTube](https://www.youtube.com/watch?v=Wj1FfilAe2Y), [Doc VehicleBody3D](https://docs.godotengine.org/en/stable/classes/class_vehiclebody3d.html), [Asset Library](https://godotengine.org/asset-library/asset/1879) |

## Journal de bord

Le suivi détaillé de l'avancement du projet : [Journal de bord](https://docs.google.com/document/d/1H4nKOKKnprnX5YsHTaftCW5wJqea41p1ubrGYx5cAO8)