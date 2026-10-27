# 5. Checklist de tests

## A. Avant tout déploiement (PC)

- [ ] `node --test web/tests/*.test.js` : 14/14 au vert (logique, manifeste, privilèges, fichiers, syntaxe ES5)
- [ ] UI en mock (`http://localhost:5173`) : navigation dans les 4 directions, sans perte de focus
- [ ] Jaune/F3 épingle puis désépingle ; Bleu/F4 masque ; Vert/F2 + gauche/droite réordonne
- [ ] Rechargement de la page : les préférences sont conservées
- [ ] *Réinitialiser l'accueil* rétablit les favoris par défaut
- [ ] `version` incrémentée dans `config.xml` si c'est une mise à jour
- [ ] Le tag git de la version précédente existe (pour le rollback)

## B. Premier lancement sur la TV

- [ ] `tv.ps1 -Action deploy` se termine sans erreur
- [ ] L'accueil s'affiche en **moins de 2 s**
- [ ] Inspector : aucune erreur rouge, log `[QuietHome] démarré (TV)`
- [ ] La rangée « Applications » liste vos apps (sinon : erreur `getAppsInfo` dans la console)
- [ ] Les touches couleur réagissent (sinon : `registerKey` échoué dans la console)
- [ ] Chaque favori se lance ; un favori grisé correspond bien à une app absente
- [ ] Retour depuis une app → l'accueil réapparaît et la liste est rafraîchie
- [ ] Touche Retour sur l'accueil → retour à la TV/source, sans plantage
- [ ] Touche HOME → le Smart Hub s'ouvre normalement (rollback toujours accessible)

## C. Démarrage

- [ ] Autorun Last App ON : extinction puis rallumage → Quiet Home s'ouvre
- [ ] Débranchement secteur 30 s → même comportement
- [ ] Le rendu est immédiat grâce au cache, puis la liste se met à jour

## D. Stabilité (1 semaine)

- [ ] Aucun gel ni écran noir à l'ouverture
- [ ] La mémoire n'augmente pas après 20 allers-retours app ↔ accueil (Inspector → Memory)
- [ ] Après une mise à jour firmware : l'app est toujours installée et Autorun toujours actif (à revérifier)
- [ ] Le certificat n'est pas expiré (noter sa date d'expiration)

## E. Critères d'arrêt (rollback immédiat)

- L'accueil empêche d'accéder au Smart Hub ou aux réglages : désinstaller
- Redémarrages en boucle ou gel : débrancher, puis désinstaller ; reset factory en dernier recours
- Échec répété du lancement d'apps tierces après une mise à jour : revenir à la version taguée précédente
