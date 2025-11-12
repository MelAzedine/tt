# 🎮 Générateur de Personnage Call of Duty

## Description

Le **Générateur de Personnage Call of Duty** est une nouvelle fonctionnalité intégrée à ARTHEMIS CONTROL qui permet de créer des personnages personnalisés pour vos sessions de jeu Call of Duty. Cette fonctionnalité génère des configurations complètes de personnages incluant :

- ✅ Noms et surnoms tactiques authentiques
- ✅ Classes de combat (Assault, Support, Recon, Engineer)
- ✅ Loadouts d'armes basés sur les armes disponibles dans trident.json
- ✅ Perks et avantages tactiques
- ✅ Séries d'élimination (killstreaks)
- ✅ Statistiques de personnage (Vitesse, Santé, Armure, Précision)
- ✅ Options d'apparence (camouflages, insignes)

## 🚀 Accès à la Fonctionnalité

### Depuis l'application
1. Ouvrir **ARTHEMIS CONTROL**
2. Cliquer sur **"Outils"** dans le menu principal
3. Dans la fenêtre Outils, section **"Générateur"**, cliquer sur **"🎮 Personnage CoD"**

## 🎯 Utilisation

### Génération Aléatoire
1. Cliquer sur le bouton **"🎲 Générer Personnage Aléatoire"**
2. Le système génère automatiquement :
   - Un nom complet avec surnom tactique (ex: Alex "Shadow" Price)
   - Une classe adaptée
   - Un loadout d'armes cohérent
   - Des perks équilibrés
   - Des statistiques basées sur la classe choisie

### Personnalisation Manuelle
Vous pouvez également personnaliser manuellement chaque aspect :

#### 📋 Informations de Base
- **Nom du personnage** : Entrez votre propre nom ou utilisez le générateur
- **Classe** : 
  - *Assault* - Combattant de première ligne (stats équilibrées offensives)
  - *Support* - Spécialiste du soutien (santé et armure élevées)
  - *Recon* - Éclaireur et tireur d'élite (vitesse et précision élevées)
  - *Engineer* - Expert en explosifs (stats équilibrées)
- **Rang** : De Recrue à Colonel

#### 🔫 Armement
- **Arme principale** : Choisissez parmi toutes les armes disponibles dans votre configuration
- **Arme secondaire** : Pistolets et armes de mêlée
- **Équipement tactique** : Grenades flash, fumigènes, capteurs, etc.
- **Équipement létal** : Grenades à fragmentation, C4, mines, etc.

#### ⭐ Perks / Avantages
Trois slots de perks pour personnaliser votre style de jeu :
- **Perk 1** : Mobilité et furtivité (Léger, Furtif, Scavenger, EOD)
- **Perk 2** : Gameplay stratégique (Ghost, Hardline, Overkill, Restock)
- **Perk 3** : Avantages tactiques (Amplifié, Ninja, Spotter, Battle Hardened)

#### 🎯 Séries d'Élimination
Configurez vos killstreaks :
- **3 éliminations** : UAV, Drone de reconnaissance, Bombe de groupe
- **5 éliminations** : Précision aérienne, Tourelle sentinelle, Raid aérien
- **7 éliminations** : Hélicoptère de combat, Paquet de ravitaillement, Missile guidé

#### 📊 Statistiques
Les statistiques sont générées automatiquement en fonction de la classe :
- **Vitesse** : Mobilité et rapidité de déplacement (0-100)
- **Santé** : Points de vie et endurance (0-100)
- **Armure** : Résistance aux dégâts (0-100)
- **Précision** : Visée et contrôle des armes (0-100)

#### 👤 Apparence
- **Camouflage** : Urbain, Forêt, Désert, Arctique, Numérique
- **Insigne d'unité** : Aigle de guerre, Crâne tactique, Éclair, Cible, Flammes

## 💾 Sauvegarde et Export

### Sauvegarde Automatique
- Cliquer sur **"💾 Sauvegarder"**
- Le personnage est enregistré dans `Mes Documents\Trident_Characters\`
- Nom du fichier : `[Nom_du_personnage]_[Date_Heure].json`

### Export Manuel
- Cliquer sur **"📤 Exporter"**
- Choisir le format :
  - **JSON** : Format structuré pour importation future
  - **TXT** : Format texte lisible pour partage rapide
- Choisir l'emplacement de sauvegarde

### Structure du Fichier JSON
```json
{
  "Name": "Alex \"Shadow\" Price",
  "Class": "Assault - Combattant de première ligne",
  "Rank": "Sergent",
  "PrimaryWeapon": "XM4",
  "SecondaryWeapon": "Pistolet 9mm",
  "Tactical": "Grenade Flash",
  "Lethal": "Grenade à fragmentation",
  "Perk1": "Léger - Vitesse de mouvement +10%",
  "Perk2": "Ghost - Invisible aux UAV",
  "Perk3": "Amplifié - Meilleure audition",
  "Killstreak1": "UAV - Radar aérien",
  "Killstreak2": "Précision Aérienne",
  "Killstreak3": "Hélicoptère de combat",
  "Camouflage": "Urbain - Gris/Noir",
  "Emblem": "🦅 Aigle de guerre",
  "Stats": {
    "Speed": 75,
    "Health": 85,
    "Armor": 70,
    "Accuracy": 70
  },
  "CreatedDate": "2025-01-08 12:30:45"
}
```

## 🎲 Algorithme de Génération

### Noms et Surnoms
Le générateur utilise une base de noms inspirés de personnages emblématiques de Call of Duty :
- **Prénoms** : Alex, Mason, Woods, Hudson, Price, Ghost, Soap, etc.
- **Noms de famille** : Price, MacTavish, Riley, Garrick, Mitchell, etc.
- **Surnoms tactiques** : Shadow, Reaper, Phantom, Viper, Titan, Wolf, etc.

Format : `[Prénom] "[Surnom]" [Nom de famille]`

### Statistiques par Classe

#### Assault (Combattant de première ligne)
- Vitesse : 65-85 (moyenne haute)
- Santé : 75-95 (haute)
- Armure : 60-80 (moyenne haute)
- Précision : 60-80 (moyenne haute)

#### Support (Spécialiste du soutien)
- Vitesse : 55-75 (moyenne)
- Santé : 85-95 (très haute)
- Armure : 80-90 (très haute)
- Précision : 55-75 (moyenne)

#### Recon (Éclaireur et tireur d'élite)
- Vitesse : 80-90 (très haute)
- Santé : 60-80 (moyenne)
- Armure : 50-70 (basse)
- Précision : 90-100 (maximale)

#### Engineer (Expert en explosifs)
- Vitesse : 60-80 (moyenne haute)
- Santé : 65-85 (moyenne haute)
- Armure : 65-85 (moyenne haute)
- Précision : 60-90 (variable)

## 📁 Emplacements des Fichiers

### Dossier de Sauvegarde
```
C:\Users\[VotreNom]\Documents\Trident_Characters\
```

### Configuration des Armes
Les armes disponibles sont chargées depuis :
```
[Dossier_Application]\trident.json
```

## 🔧 Personnalisation Avancée

### Modifier les Armes Disponibles
Les armes affichées dans le générateur sont automatiquement synchronisées avec votre fichier `trident.json`. Pour ajouter de nouvelles armes :

1. Ouvrir `trident.json`
2. Ajouter une nouvelle entrée dans la section `"Profiles"`
3. Redémarrer le générateur de personnage

### Ajouter de Nouveaux Noms
Les noms sont définis dans `CharacterGeneratorWindow.xaml.cs`. Pour personnaliser :
- Modifier les tableaux `_firstNames`, `_lastNames`, `_callsigns`

## ❓ FAQ

**Q: Les personnages générés sont-ils sauvegardés automatiquement ?**  
R: Non, vous devez cliquer sur "Sauvegarder" ou "Exporter" pour enregistrer votre personnage.

**Q: Puis-je importer un personnage sauvegardé ?**  
R: Actuellement, la fonction d'import n'est pas disponible. Vous pouvez ouvrir le fichier JSON pour voir les détails.

**Q: Les statistiques affectent-elles le gameplay ?**  
R: Non, les statistiques sont purement cosmétiques et servent à créer des profils de personnages pour l'immersion.

**Q: Combien de personnages puis-je créer ?**  
R: Illimité ! Tous les personnages sont sauvegardés dans `Documents\Trident_Characters\`.

**Q: Puis-je partager mes personnages avec des amis ?**  
R: Oui ! Exportez en JSON et partagez le fichier. Votre ami peut l'ouvrir avec n'importe quel éditeur de texte.

## 🎨 Captures d'écran

*Note : Cette fonctionnalité nécessite Windows pour être visualisée. L'interface utilise le thème sombre ARTHEMIS CONTROL avec des effets glassmorphism.*

## 🔄 Mises à Jour Futures

Fonctionnalités prévues :
- [ ] Import de personnages depuis JSON
- [ ] Galerie de personnages sauvegardés
- [ ] Comparaison de personnages
- [ ] Présets de loadouts populaires
- [ ] Partage communautaire de personnages
- [ ] Statistiques détaillées (K/D ratio, temps de jeu fictif, etc.)

## 📝 Notes Techniques

- **Framework** : WPF (.NET 8.0)
- **Langage** : C# avec XAML
- **Dépendances** : Aucune dépendance externe supplémentaire
- **Format de sauvegarde** : JSON (UTF-8)
- **Compatibilité** : Windows 10/11

## 🤝 Contribution

Pour suggérer des améliorations ou signaler des bugs :
1. Ouvrir une issue sur GitHub
2. Proposer de nouveaux noms de personnages
3. Suggérer de nouveaux perks ou équipements

---

**Amusez-vous bien avec le Générateur de Personnage Call of Duty ! 🎮**
