# 2. Choix technique et architecture

## Recommandation

**Tizen Web App (HTML/CSS/JS ES5, sans framework ni build), sideloadée en mode développeur, ouverte au démarrage grâce au réglage natif « Autorun Last App ».**

| Avantages | Inconvénients |
|---|---|
| Aucun root, aucune modification du firmware : **zéro risque de brick** | Ne remplace pas *techniquement* le Smart Hub : la touche HOME ouvre toujours la barre Samsung |
| Désinstallation = retour exact à l'état d'origine | Pas de bascule HDMI depuis l'app (privilège partner) : on utilise la touche SOURCE |
| API publiques et stables depuis Tizen 2.3 : fonctionne sur la plupart des modèles 2016+ | Le certificat Samsung de dev est lié au DUID de la TV et expire (à renouveler) |
| Démarrage très rapide (aucune dépendance, environ 20 Ko) | « Autorun Last App » n'existe pas sur tous les modèles et n'est pas garanti après une mise à jour firmware |
| Développement et tests sur PC (mock) | Les icônes des autres apps peuvent être inaccessibles (sandbox) : repli sur des initiales colorées |

**Évolution possible (optionnelle, plus tard)** : si vous voulez un vrai remplacement du Home, la voie quiet-tizen (root) peut s'ajouter **par-dessus** cette app sans la changer. Elle n'est à tenter qu'après validation complète et avec l'acceptation explicite du risque (voir [06-securite-compatibilite.md](06-securite-compatibilite.md)).

## Architecture

```
src/
├── config.xml        Manifeste Tizen : id, 2 privilèges publics, aucun accès réseau
├── index.html        Squelette + ordre de chargement des scripts
├── icon.png
├── css/style.css     Mise en page fixe 1920x1080, animations transform/opacity uniquement
└── js/
    ├── defaults.js   Configuration par défaut (favoris, filtres)        ← pur
    ├── store.js      Préférences + cache (localStorage, tolérant aux erreurs) ← pur
    ├── catalog.js    Apps installées + prefs → rangées ; pin/hide/move  ← pur
    ├── focus.js      Navigation spatiale par rangées                    ← pur
    ├── platform.js   SEUL module qui touche aux API Tizen (+ mock PC)
    ├── ui.js         Rendu DOM, horloge, toasts
    └── main.js       Démarrage, touches, cycle de vie
```

### Principes

- **Couche plateforme isolée** : `platform.js` encapsule `tizen.*`. Ailleurs, le code ne connaît qu'une interface (`listApps`, `launch`, `hide`, `registerKeys`). Le mock permet de développer dans Chrome.
- **Logique pure testable** sous Node (`web/tests/logic.test.js`), sans TV.
- **Services** : aucun service en arrière-plan (`background-support="disable"`). Rien ne tourne quand l'accueil est masqué.
- **Configuration** : `defaults.js` (versionnée) fusionnée avec les préférences utilisateur (`localStorage`, clé `qh.prefs.v1`). La fusion ignore les champs inconnus ou mal typés, et un JSON corrompu ramène aux défauts.
- **Cache** : la liste des apps est mise en cache (`qh.apps.v1`). Au démarrage, l'affichage est immédiat depuis le cache, puis la liste réelle est rechargée en asynchrone (stale-while-revalidate). Elle est aussi rechargée à chaque retour sur l'accueil (`visibilitychange`).
- **Robustesse des favoris** : les IDs d'apps Samsung changent selon l'année et la région. Chaque favori liste plusieurs IDs candidats plus un motif de nom en secours. Un favori introuvable reste affiché grisé, ce qui garde une mise en page stable.
- **Mises à jour** : incrémenter `version` dans `config.xml`, puis `tv.ps1 -Action deploy`. Une réinstallation conserve `localStorage`, une désinstallation l'efface. Aucune mise à jour automatique : c'est volontaire, pour garder le contrôle sur une TV de test.
- **Vie privée** : aucune balise `<access>`, donc aucune requête réseau possible depuis l'app.

### Commandes télécommande

| Touche | Action |
|---|---|
| Flèches / OK | Naviguer / lancer |
| Jaune | Épingler ou désépingler l'app sélectionnée |
| Bleu | Masquer l'app de la bibliothèque |
| Vert | Mode édition (gauche/droite déplace un favori) |
| Rouge | Recharger la liste des apps |
| Retour | Quitter le mode édition, sinon masquer l'accueil |

Sur PC : F1 à F4 remplacent Rouge/Vert/Jaune/Bleu, Échap remplace Retour.
