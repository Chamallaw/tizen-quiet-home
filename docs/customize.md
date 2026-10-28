# Personnaliser

[English](en/customize.md) · **Français**

## Depuis la télécommande

| Action | Où |
|---|---|
| Ajouter ou retirer un favori | OK maintenu sur une tuile, ou *Toutes les apps* → OK maintenu |
| Réordonner les favoris | OK maintenu → *Déplacer*, puis ◀ ▶ et OK |
| Masquer une app | OK maintenu → *Masquer* (Récupération → *Réafficher les apps masquées* pour annuler) |
| Changer de thème | Bouton ◐ en haut à droite, ou Réglages → *Thème* avec ◀ ▶ : *Automatique*, puis chacun des 5 thèmes (mémorisé) |
| Son du curseur | Réglages → *Son du curseur*, ◀ ▶ : Goutte, Bois, Kalimba ou Désactivé. Le son choisi est joué à chaque changement |
| Bouton Home | Réglages → *Bouton Home* |
| Afficher ou masquer la météo | Réglages → *Météo* (si un lieu est indiqué) |
| Langue | Réglages → *Langue*, ◀ ▶ : *Automatique* (celle de la TV), Français ou English |
| Volume | Réglages → *Volume* : ◀ ▶ règle, OK coupe ou rétablit le son |
| Appareils HDMI | Réglages → *Entrées HDMI* : pour chaque prise, ◀ ▶ choisit l'appareil (voir ci-dessous) |
| Repartir de zéro | Réglages → Récupération et diagnostic → *Réinitialiser la configuration* |

Le panneau Réglages a deux parties :
- **Télévision** : volume, paramètres de la TV (ceux de Samsung) et accueil Samsung (Smart Hub) ;
- **Quiet Home** : thème, son du curseur, météo, bouton Home, langue, entrées HDMI, puis *Récupération et diagnostic*.

Chaque réglage affiche sa valeur à droite et se change sur place avec ◀ ▶ (ou OK), sans fermer le panneau.

La configuration est stockée sur la TV, dans le dossier `data` de l'app. Elle est conservée lors des mises à jour et effacée à la désinstallation.

## Langue

L'interface existe en **français** et en **anglais**. En *Automatique* (par défaut), Quiet Home est en français si la TV est en français, en anglais sinon ; il revérifie au retour des paramètres de la TV. Changer la valeur dans Réglages → *Langue* redessine tout l'écran aussitôt : rangées, date, salutation, météo et panneaux.

Les traductions sont dans `native/src/QuietHome.Core/Lang.cs` : le texte français sert de clé, la table donne le texte anglais. Un texte absent de la table reste en français. Un test vérifie que chaque entrée garde ses trous `{0}`.

## Favoris et appareils par défaut

Ils sont définis dans `native/src/QuietHome.Core/DefaultConfig.cs` :

- **Favoris** : chaque `PinSpec` liste des identifiants Tizen candidats et un mot-clé de secours, cherché dans le nom de l'app. Pour trouver les identifiants de votre TV : Réglages → Récupération → **Diagnostic**, qui liste toutes les apps installées avec leur identifiant.
- **Appareils HDMI** : PS5 sur HDMI 2 et Switch sur HDMI 4 par défaut (`InputSpec`). Ils se changent dans l'app, sans toucher au code (voir *Entrées HDMI*).
- Une configuration déjà enregistrée sur la TV n'est pas écrasée. Les ajouts aux valeurs par défaut se font par **migration versionnée** (`ConfigStore.Normalize`, `LauncherConfig.CurrentVersion`).

## Entrées HDMI

Réglages → *Entrées HDMI* liste les prises HDMI 1 à 4. Sur chaque ligne, ◀ ▶ choisit ce qui est branché :

| Appareil | Sur l'accueil |
|---|---|
| Rien | aucune tuile |
| PS5, Switch, Xbox | tuile au logo de la console |
| Apple TV, Shield | tuile au logo de la marque |
| Décodeur TV, Barre de son (avec entrée HDMI), PC, Blu-ray, Appareil | tuile à son nom, avec un pictogramme |

Le changement est enregistré tout de suite et la rangée de l'accueil suit. Elle s'appelle *Consoles* quand elle ne contient que des consoles, *Appareils* sinon. Les tuiles sont rangées par numéro de prise et indiquent « HDMI n ». OK sur une tuile bascule la TV sur cette prise.

Les types proposés sont définis dans `HdmiPorts.Kinds` (`native/src/QuietHome.Core/HdmiPorts.cs`). La prise n correspond à l'identifiant de source `12 + n` passé à `connect_source` (13 = HDMI 1 sur les modèles testés). Pour vérifier : Diagnostic → *Tester HDMI 1…4*.

## Thème automatique

Par défaut, l'accueil passe en **Hinoki** (clair) le jour et en **Kuri** (noyer) le soir, avec un fondu :
- avec un lieu indiqué (voir [Météo](#météo)), la bascule suit le lever et le coucher du soleil, calculés sur la TV sans réseau ;
- sans lieu, le jour va de 7 h à 19 h.

Les thèmes du jour et du soir sont `DayTheme` et `NightTheme` dans `DefaultConfig.cs`. Le bouton ◐ fixe un thème. Il repasse sur *Automatique* après le cinquième thème. L'écran de veille reste sombre et garde le mur de bois, même avec un thème clair.

## Météo

Un bandeau discret en bas de l'accueil montre les **24 prochaines heures** : l'heure, un pictogramme et la température. Les prévisions viennent d'[Open-Meteo](https://open-meteo.com), sans compte ni clé. La TV fait une requête par heure au plus, et la dernière réponse reste en cache : le bandeau s'affiche dès le démarrage et tient une journée sans réseau.

La météo ne s'active que si vous indiquez un lieu, dans `.local/quiethome.json`. Ce fichier reste sur votre PC : `.local/` n'est jamais publié.

```json
{ "Place": "Lyon", "Latitude": 45.76, "Longitude": 4.84 }
```

`tv-native.ps1 -Action deploy` l'embarque dans le paquet (`res/local.json`). Pour retirer le lieu, remplacez son contenu par `{}`, puis redéployez. Les coordonnées d'une ville se trouvent par exemple avec la recherche d'[Open-Meteo](https://open-meteo.com/en/docs/geocoding-api). Le bandeau se masque dans Réglages → *Météo*. Il disparaît aussi si la rangée *Bibliothèque* est activée, faute de place.

## Logos et couleurs des marques

- **Couleurs et correspondance libellé → logo** : `Themes.Brands`, dans `native/src/QuietHome.Core/Themes.cs`.
- **Logos** : `native/tools/fetch_marks.py` télécharge les SVG depuis Simple Icons v13.21.0 (CC0) et Custom Brand Icons (MIT), puis `native/tools/render_marks.py <chrome.exe>` les convertit en PNG blancs dans `native/res/icons/`, à leur taille d'affichage exacte : `<nom>.png` pour les cartes, `<nom>-hdmi.png` pour les tuiles d'appareils. L'app les colore au moment de l'affichage.
- Une marque sans logo disponible garde sa couleur et affiche son nom. C'est le cas de Free TV, CANAL+ et ARTE.

## Thèmes, fonds, polices

| | Fichier / outil |
|---|---|
| Palettes des 5 thèmes | `Themes.All`, dans `Themes.cs` |
| Fonds bois 4K (Kuri, Chêne fumé) | `native/tools/make_wallpaper.py <chrome.exe>` : veinage procédural, voile de lisibilité intégré |
| Pictogrammes (apps, réglages, thème, météo, appareils) et logo de l'app | `native/tools/make_icons.py`, à leur taille d'affichage exacte |
| Sons du curseur (Goutte, Bois, Kalimba) | `native/tools/make_sounds.py` : sinusoïdes pures, courtes (0,11 à 0,15 s), attaque arrondie, niveau bas |
| Police DM Sans (OFL) | `native/tools/fetch_fonts.py`, enregistrée au démarrage via `FontClient.AddCustomFontDirectory` |
| Rendus de l'accueil (5 thèmes), de la grille, des réglages et de la veille | `native/tools/make_preview_kuri.py [dossier] [fr\|en]`, puis capture avec Chrome headless |

Toutes les ressources générées sont déjà dans `native/res/`. Ces outils ne servent qu'à les modifier.
