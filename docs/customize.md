# Personnaliser

## Depuis la télécommande

| Action | Où |
|---|---|
| Ajouter ou retirer un favori | OK maintenu sur une tuile, ou *Toutes les apps* → OK maintenu |
| Réordonner les favoris | OK maintenu → *Déplacer*, puis ◀ ▶ et OK |
| Masquer une app | OK maintenu → *Masquer* (Récupération → *Réafficher les apps masquées* pour annuler) |
| Changer de thème | Bouton ◐ en haut à droite (mémorisé) |
| Bouton Home | Réglages → *Bouton Home* |
| Repartir de zéro | Réglages → Récupération → *Réinitialiser la configuration* |

La configuration est stockée sur la TV, dans le dossier `data` de l'app. Elle est conservée lors des mises à jour et effacée à la désinstallation.

## Favoris et consoles par défaut

Ils sont définis dans `native/src/QuietHome.Core/DefaultConfig.cs` :

- **Favoris** : chaque `PinSpec` liste des identifiants Tizen candidats et un mot-clé de secours, cherché dans le nom de l'app. Pour trouver les identifiants de votre TV : Réglages → Récupération → **Diagnostic**, qui liste toutes les apps installées avec leur identifiant.
- **Consoles** : `InputSpec("PS5", 14)`. Le nombre est l'identifiant de source passé à `connect_source` (13 = HDMI 1, …, 16 = HDMI 4 sur les modèles testés). Pour calibrer : Diagnostic → *Tester HDMI 1…4*.
- Une configuration déjà enregistrée sur la TV n'est pas écrasée. Les ajouts aux valeurs par défaut se font par **migration versionnée** (`ConfigStore.Normalize`, `LauncherConfig.CurrentVersion`).

## Logos et couleurs des marques

- **Couleurs et correspondance libellé → logo** : `Themes.Brands`, dans `native/src/QuietHome.Core/Themes.cs`.
- **Logos** : `native/tools/fetch_marks.py` télécharge les SVG depuis Simple Icons v13.21.0 (CC0) et Custom Brand Icons (MIT), puis `native/tools/render_marks.py <chrome.exe>` les convertit en PNG blancs dans `native/res/icons/`. L'app les colore au moment de l'affichage.
- Une marque sans logo disponible garde sa couleur et affiche son nom. C'est le cas de Free TV, CANAL+ et ARTE.

## Thèmes, fonds, polices

| | Fichier / outil |
|---|---|
| Palettes des 5 thèmes | `Themes.All`, dans `Themes.cs` |
| Fonds bois 4K (Kuri, Chêne fumé) | `native/tools/make_wallpaper.py <chrome.exe>` : veinage procédural, voile de lisibilité intégré |
| Pictogrammes (apps, réglages, thème) et logo de l'app | `native/tools/make_icons.py` |
| Police DM Sans (OFL) | `native/tools/fetch_fonts.py`, enregistrée au démarrage via `FontClient.AddCustomFontDirectory` |
| Rendus de l'accueil, de la grille et de la veille | `native/tools/make_preview_kuri.py`, puis capture avec Chrome headless |

Toutes les ressources générées sont déjà dans `native/res/`. Ces outils ne servent qu'à les modifier.
