# Personnaliser

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
| Volume | Réglages → *Volume* : ◀ ▶ règle, OK coupe ou rétablit le son |
| Repartir de zéro | Réglages → Récupération et diagnostic → *Réinitialiser la configuration* |

Le panneau Réglages a deux parties :
- **Télévision** : volume, paramètres de la TV (ceux de Samsung) et accueil Samsung (Smart Hub) ;
- **Quiet Home** : thème, son du curseur, météo, bouton Home, puis *Récupération et diagnostic*.

Chaque réglage affiche sa valeur à droite et se change sur place avec ◀ ▶ (ou OK), sans fermer le panneau.

La configuration est stockée sur la TV, dans le dossier `data` de l'app. Elle est conservée lors des mises à jour et effacée à la désinstallation.

## Favoris et consoles par défaut

Ils sont définis dans `native/src/QuietHome.Core/DefaultConfig.cs` :

- **Favoris** : chaque `PinSpec` liste des identifiants Tizen candidats et un mot-clé de secours, cherché dans le nom de l'app. Pour trouver les identifiants de votre TV : Réglages → Récupération → **Diagnostic**, qui liste toutes les apps installées avec leur identifiant.
- **Consoles** : `InputSpec("PS5", 14)`. Le nombre est l'identifiant de source passé à `connect_source` (13 = HDMI 1, …, 16 = HDMI 4 sur les modèles testés). Pour calibrer : Diagnostic → *Tester HDMI 1…4*.
- Une configuration déjà enregistrée sur la TV n'est pas écrasée. Les ajouts aux valeurs par défaut se font par **migration versionnée** (`ConfigStore.Normalize`, `LauncherConfig.CurrentVersion`).

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
- **Logos** : `native/tools/fetch_marks.py` télécharge les SVG depuis Simple Icons v13.21.0 (CC0) et Custom Brand Icons (MIT), puis `native/tools/render_marks.py <chrome.exe>` les convertit en PNG blancs dans `native/res/icons/`. L'app les colore au moment de l'affichage.
- Une marque sans logo disponible garde sa couleur et affiche son nom. C'est le cas de Free TV, CANAL+ et ARTE.

## Thèmes, fonds, polices

| | Fichier / outil |
|---|---|
| Palettes des 5 thèmes | `Themes.All`, dans `Themes.cs` |
| Fonds bois 4K (Kuri, Chêne fumé) | `native/tools/make_wallpaper.py <chrome.exe>` : veinage procédural, voile de lisibilité intégré |
| Pictogrammes (apps, réglages, thème, météo) et logo de l'app | `native/tools/make_icons.py` |
| Sons du curseur (Goutte, Bois, Kalimba) | `native/tools/make_sounds.py` : sinusoïdes pures, courtes (0,11 à 0,15 s), attaque arrondie, niveau bas |
| Police DM Sans (OFL) | `native/tools/fetch_fonts.py`, enregistrée au démarrage via `FontClient.AddCustomFontDirectory` |
| Rendus de l'accueil (5 thèmes), de la grille, des réglages et de la veille | `native/tools/make_preview_kuri.py`, puis capture avec Chrome headless |

Toutes les ressources générées sont déjà dans `native/res/`. Ces outils ne servent qu'à les modifier.
