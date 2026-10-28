# Journal des versions

Chaque version poussée avec une étiquette `vX.Y.Z` est publiée sur GitHub avec les notes ci-dessous. Elle ne contient pas de `.tpk` prêt à installer : chaque TV signe son propre paquet avec son certificat Samsung ([docs/install.md](docs/install.md)).

## 0.10.0 — 2026-10-28

- **Retour sur l'app quittée** : après le bouton Home, la sélection reprend sur la tuile de l'app qu'on vient de quitter.
- **Thème automatique** : Hinoki le jour, Kuri le soir. La bascule se fait au lever et au coucher du soleil, calculés sur la TV, ou de 7 h à 19 h si aucun lieu n'est indiqué, en fondu. Le bouton Thème permet toujours de fixer un thème. Le thème choisi auparavant devient le thème de son moment : celui du soir s'il est sombre, celui du jour s'il est clair.
- **Météo des prochaines 24 h**, heure par heure, en bas de l'accueil. Elle vient d'Open-Meteo, sans compte. Elle est mise à jour une fois par heure et gardée en cache, et n'apparaît que si un lieu est indiqué dans `.local/quiethome.json`. On peut la masquer dans Réglages.
- L'écran de veille garde le mur de bois même avec un thème clair.
- Les tests et la compilation tournent sur GitHub à chaque push, et les versions sont publiées à partir de ce journal.

## 0.9.1 — 2026-10-27

- Retour à Quiet Home au rallumage de la TV, y compris après une veille de plusieurs heures.

## 0.9.0 — 2026-10-27

Première version publique :
- launcher natif .NET/NUI ;
- favoris aux logos et couleurs officiels, et consoles ;
- cinq thèmes Japandi, dont Kuri et Chêne fumé avec un fond bois 4K ;
- bouton Home ramené à Quiet Home ;
- écran de veille, mode récupération et logs UDP.
