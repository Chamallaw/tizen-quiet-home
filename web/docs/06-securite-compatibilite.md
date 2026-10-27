# 6. Sécurité et compatibilité

## Compatibilité

| Élément | Exigence | Comment vérifier |
|---|---|---|
| Version Tizen | 2.3 minimum (`required_version`) ; ciblé Tizen 4+ (2018+) | `.\scripts\tv.ps1 -TvIp <IP> -Action info` → ligne `platform_version` |
| Moteur JS | Code ES5 uniquement (vérifié par un test) : marche aussi sur les vieux Chromium Tizen | Test « pas de syntaxe ES2015+ » |
| Résolution | Mise en page 1920x1080. La TV met à l'échelle sur les modèles 4K. | Visuel |
| Touches couleur | Absentes de certaines télécommandes « Smart » (One Remote) : utilisez le bouton **123** → touches couleur à l'écran | Test B de la checklist |
| Autorun Last App | Dépend du modèle et du pays | Menu Fonctions Smart |
| IDs des apps | Varient selon l'année et la région : ajustez `src/js/defaults.js` | Rangée « Applications » + Inspector |

**Information à me transmettre** pour affiner : la référence exacte du modèle (étiquette arrière, ex. `QE55Q80B`) et la version du firmware (*Assistance → À propos de ce téléviseur*).

## Permissions

| Privilège | Niveau | Usage |
|---|---|---|
| `application.launch` | public | lancer les apps installées |
| `tv.inputdevice` | public | touches couleur |

Aucun privilège partner ou platform. Aucun accès réseau (pas de `<access>`). Aucun stockage hors du sandbox (`localStorage` de l'app).

## Risques et mesures

| Risque | Probabilité | Impact | Mesure |
|---|---|---|---|
| Brick / instabilité système | Quasi nul (sandbox, aucune modification système) | — | Pas de root, pas de SQL système |
| Accueil inutilisable (bug JS) | Faible | Moyen | HOME reste fonctionnel ; erreurs capturées et loguées ; désinstallation |
| Mise à jour firmware qui coupe le mode dev ou Autorun | Moyenne | Faible | Réactiver le mode dev et les réglages, puis redéployer |
| Expiration du certificat | Certaine (à terme) | Faible : l'app installée continue généralement de fonctionner, mais on ne peut plus la réinstaller | Noter la date, regénérer |
| Mode dev ouvert sur le réseau | Faible | Moyen : `sdb` n'accepte que l'IP déclarée | Réseau domestique de confiance ; désactiver le mode dev hors des périodes de travail |
| Voie root (si un jour envisagée) | Élevée | Élevé (brick, perte de garantie, faille réseau) | **Hors périmètre actuel.** À n'envisager qu'avec un snapshot, sur une TV jetable, après lecture complète de quiet-tizen |
