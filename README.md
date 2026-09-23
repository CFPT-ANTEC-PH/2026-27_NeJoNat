# 2026-27_NeJoNat

# Simulation 3D de Robots - Godot

Projet de simulation 3D réalisé par **Nathan**, **Joao** et **Nelson**.

## Description du projet

Le but de ce projet est de réaliser une simulation 3D sur Godot mettant en scène des robots autonomes.

Les robots évoluent sur une table de **3 mètres par 6 mètres**, bordée de rebords de **10 centimètres**. Chaque robot est équipé de divers capteurs lui permettant de se déplacer et de suivre un chemin précis.

### Structure d'un robot

- 2 roues arrière **motorisées**, assurant la propulsion et le changement de direction
- 1 roue avant, centrée, non motorisée

### Personnalisation

Le projet vise également à permettre la personnalisation des robots via la modification de leurs attributs (par exemple les roues), chaque modèle disposant de spécificités propres.

## Outils utilisés

- **Godot** — moteur de jeu utilisé pour la simulation 3D et la programmation du comportement des robots
- **Blender** — modélisation 3D des robots

## État d'avancement

- [x] Prise en main de Godot et Blender
- [x] Modélisation 3D d'un premier robot sous Blender et import dans Godot
- [x] Création d'une carte de test
- [x] Script de déplacement de base pour un robot de test
- [ ] Correction du bug : les roues traversent le sol
- [ ] Gestion des capteurs et du suivi de chemin
- [ ] Système de personnalisation des attributs des robots

## Bugs connus

| Bug | Statut | Sources |
|---|---|---|
| Les roues passent à travers le sol | Non fixé | [Vidéo YouTube](https://www.youtube.com/watch?v=Wj1FfilAe2Y), [Doc VehicleBody3D](https://docs.godotengine.org/en/stable/classes/class_vehiclebody3d.html), [Asset Library](https://godotengine.org/asset-library/asset/1879) |

## Journal de bord

Le suivi détaillé de l'avancement du projet : [Journal de bord](https://docs.google.com/document/d/1H4nKOKKnprnX5YsHTaftCW5wJqea41p1ubrGYx5cAO8)