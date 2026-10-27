# Prototype web (historique)

> Première version, en Web App Tizen (.wgt). Elle est remplacée par le launcher natif (`../native`), mais reste utile pour les TV sans runtime .NET.

## Quiet Home Web

Accueil minimaliste, rapide et sans publicité. C'est une **Tizen Web App standard**, sans root et sans modification du système : on la désinstalle et la TV revient à son état d'origine.

Inspiré de [jss8649/quiet-tizen](https://github.com/jss8649/quiet-tizen), mais sans la partie root, qui est propre à un seul modèle (voir [docs/01-analyse.md](docs/01-analyse.md)).

## Démarrage rapide

```bash
node --test web/tests/*.test.js
```

```bash
python -m http.server 5173 --bind 127.0.0.1 --directory web/src
```

Ouvrez ensuite `http://localhost:5173` (UI avec des apps factices ; flèches, Entrée, F1 à F4, Échap).

Sur la TV (après les prérequis de [docs/04](docs/04-installation-debug-rollback.md)) :

```powershell
.\web\tv.ps1 -TvIp <IP_DE_LA_TV> -Action deploy
```

## Documentation

1. [Analyse de quiet-tizen et des alternatives](docs/01-analyse.md)
2. [Choix technique et architecture](docs/02-architecture.md)
3. [Plan de projet](docs/03-plan.md)
4. [Installation, débogage, rollback](docs/04-installation-debug-rollback.md)
5. [Checklist de tests](docs/05-checklist-tests.md)
6. [Sécurité et compatibilité](docs/06-securite-compatibilite.md)

**Launcher natif .NET/NUI :** [native/README.md](../native/README.md). **Recherche :** [rapport de recherche](../docs/research/rapport-faisabilite-natif.md). Fiche TV en lecture seule : `tools/tv-fiche.ps1`.

## Arborescence

```
src/        l'app installée sur la TV (seul dossier empaqueté)
tests/      tests Node (logique + validation du paquet)
tv.ps1       : connect | build | deploy | run | debug | uninstall | info
tools/      make_icon.py (régénère src/icon.png)
docs/       livrables
```
