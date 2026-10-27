# 4. Installation, débogage et restauration

## Prérequis sur le PC (une seule fois)

1. **Tizen Studio** (avec CLI) dans `C:\tizen-studio`. Dans *Package Manager* :
   - *Extension SDK* → **Samsung Certificate Extension** et **TV Extensions** (la version la plus récente).
2. **Node.js** (déjà présent, v22) pour les tests.
3. Le pare-feu Windows doit autoriser `sdb.exe` sur le réseau privé.

## Préparer la TV

1. Notez l'IP de la TV : *Paramètres → Général → Réseau → État du réseau → Paramètres IP*.
2. Activez le mode développeur : ouvrez **Apps**, tapez **1 2 3 4 5** à la télécommande, mettez *Developer mode* sur **On** et saisissez **l'IP de ce PC**.
3. **Redémarrez la TV** (maintenez le bouton marche jusqu'au logo). La mention *Developer Mode* apparaît alors en haut de l'écran Apps.

## Certificat Samsung (une seule fois, à renouveler à expiration)

1. `sdb connect <IP_TV>`, puis `sdb devices` doit lister `<IP_TV>:26101`.
2. Tizen Studio → *Tools → Certificate Manager → +* → **Samsung** → **TV** → nom du profil : `QuietHomeTV`.
3. Créez le certificat d'auteur, connectez votre compte Samsung, puis créez le certificat distributeur **Public** avec le **DUID de la TV** (récupéré automatiquement si la TV est connectée par `sdb`).
4. Dans le Device Manager de Tizen Studio : clic droit sur la TV → **Permit to install applications** (pousse le certificat sur la TV).

## Installer / mettre à jour

```bash
node --test web/tests/*.test.js
```

```powershell
.\web\tv.ps1 -TvIp <IP_DE_LA_TV> -Action deploy -CertProfile QuietHomeTV
```

`deploy` enchaîne : connexion → copie de `src/` dans `dist/app` → signature `.wgt` → installation → lancement.

### Démarrage automatique (réglages natifs, réversibles)

Les noms varient selon l'année du modèle. Cherchez-les sous *Paramètres → Général → Fonctions Smart* :
- **Autorun Last App** (« Exéc. auto dernière app ») : **On**. Lancez Quiet Home puis éteignez la TV : elle se rallumera dessus.
- **Autorun Smart Hub** : **Off**.
- Pour réduire les contenus promotionnels sans root : désactivez les recommandations et la publicité ciblée (*Conditions et confidentialité*), ainsi que l'aperçu automatique des chaînes.
- Épinglez Quiet Home en **première position** de la barre Apps (Apps → Paramètres → Déplacer).

## Déboguer

| Besoin | Commande / méthode |
|---|---|
| UI sans TV | `python -m http.server 5173 --bind 127.0.0.1 --directory src`, puis Chrome sur `http://localhost:5173` (mock automatique ; F1 à F4 = touches couleur) |
| Web Inspector sur la TV | `.\web\tv.ps1 -TvIp <IP> -Action debug`, puis ouvrir l'URL affichée dans Chrome. Console, réseau et DOM sont disponibles. Tous les logs sont préfixés `[QuietHome]`. |
| Alternative IDE | Tizen Studio → clic droit sur le projet → *Debug As → Tizen Web Application* |
| État de l'install | `.\web\tv.ps1 -TvIp <IP> -Action info` |
| L'app ne démarre pas | Inspector → onglet Console : une erreur de script stoppe le rendu. Vérifiez aussi `node --test` (test « fichiers référencés présents »). |
| Erreur de signature / « Install failed (code -12/-11) » | Certificat expiré ou DUID différent. Recréez le profil distributeur, puis refaites *Permit to install*. |

Note : `sdb dlog` n'est en général **pas** disponible sur une TV grand public. Le Web Inspector est la source de logs fiable.

## Rollback (du plus léger au plus radical)

1. **Revenir au Smart Hub tout de suite** : touche **HOME**. Le Smart Hub n'est jamais modifié.
2. **Réinitialiser l'accueil** : tuile *Système → Réinitialiser l'accueil* (efface les préférences locales).
3. **Revenir à une version précédente** : `git checkout <tag>`, puis `tv.ps1 -Action deploy`.
4. **Désinstaller** : `.\web\tv.ps1 -TvIp <IP> -Action uninstall`, ou depuis la TV (Apps → Quiet Home → Supprimer).
5. **Rétablir les réglages** : Autorun Last App → Off, Autorun Smart Hub → On.
6. **Quitter le mode développeur** : Apps → 12345 → Off → redémarrer.
7. **Dernier recours** : *Paramètres → Assistance → Diagnostic automatique → Réinitialiser* (reset factory). Il n'est jamais nécessaire pour cette app, puisqu'elle ne modifie rien hors de son sandbox.
