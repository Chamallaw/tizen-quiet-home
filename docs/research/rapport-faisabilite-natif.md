# Rapport de recherche : faisabilité d'un launcher natif .NET/NUI

*Rapport de recherche initial (27/10/2026), écrit avant d'avoir accès à la TV. Conservé pour l'historique : la suite (sdb shell fermé sur Tizen 6.5, bouton Home par observation du premier plan) est décrite dans [../home-button.md](../home-button.md).*

## Verdict en bref

| Question | Réponse |
|---|---|
| Une app native .NET/NUI (.tpk) peut-elle tourner sur une TV Samsung en mode développeur ? | **Oui, sur les modèles 2018+ (Tizen 4.0+) qui embarquent le runtime .NET.** C'est démontré sur un QN90B 2022 (runtime .NET Core 3.1.3) et sur un U8000F 2025 (.NET 6.0.9). |
| Peut-elle devenir le Home (bouton Home) ? | **Oui, sur certains firmwares, et sans root**, en réécrivant deux clés vconf via `sdb shell`. Cela dépend entièrement de ce que votre firmware autorise : c'est le **point go/no-go n° 1**. |
| Peut-elle s'ouvrir au démarrage ? | **Incertain.** `startup_appid` a fonctionné sur l'U8000F mais **pas** sur le QN90B (la TV a repris la dernière source HDMI). Plans B : *Autorun Last App*, puis règles SmartThings. |
| Le retour à l'état Samsung est-il possible ? | **Oui**, si les valeurs d'origine sont sauvegardées **avant** toute modification et que le mode développeur reste actif. Le reset factory reste le filet de sécurité final. |
| Faut-il le root ? | **Non** pour notre cible. Le root (voie quiet-tizen d'origine) est écarté. |

## 1. Fiche technique de votre TV : à compléter

Je n'ai ni la référence du modèle ni accès à la TV. Lancez le script **en lecture seule** :

```powershell
.\tools\tv-fiche.ps1 -TvIp <IP_DE_LA_TV>
```

| Champ | Source | Valeur |
|---|---|---|
| Modèle exact (ex. `QE55Q80B`) | REST `:8001/api/v2/` → `modelName` | *à collecter* |
| Firmware (ex. `T-PTMAKUC-1720.7`) | Paramètres → Assistance → À propos | *à relever à la main* |
| Version Tizen | `sdb capability` → `platform_version` | *à collecter* |
| Architecture / CPU | `sdb capability` → `cpu_arch` ; `uname -m` | *à collecter* |
| Runtime .NET | `readlink -f /usr/share/dotnet.tizen/netcoreapp` | *à collecter* (peut répondre `closed`) |
| `home_appid` / `startup_appid` d'origine | `vconftool get db/appfw/...` | *à collecter* : **ce sont les valeurs de restauration** |
| `vconftool` autorisé dans `sdb shell` ? | exit code des lectures ci-dessus | **go/no-go** |

**Règle d'interprétation :**
- Si `vconftool get` renvoie une valeur, la voie A (section 4) est ouverte.
- S'il renvoie `closed`, le Home ne peut pas être routé sans un accès plus profond. On se replie alors sur un launcher natif lancé par *Autorun Last App* + le bouton Home Samsung, comme dans la version Web actuelle.

## 2. Comparaison des projets

| | **quiet-tizen** (jss8649) | **quiet-tizen-qn90b** (gus-cardoso) | **Fresh Home** (KSWORK2001) | **Skylight** (SaudSatopay) | **Free My TV** (simbasjungles) |
|---|---|---|---|---|---|
| Type d'app | .NET/NUI | **.NET/NUI, TPK autonome** | Web (.wgt) ×2 | Web (.wgt) + service Node | Web (.wgt) |
| Package | Remplace la DLL de `com.samsung.tv.bixby-promotion` | Paquet propre `QuietHome1` / `QuietHome1.Home` | `freshhome.launcher` + wrapper `fhkey0001.key` | `Skylight` | — |
| Signature | Hérite du conteneur Samsung | Certificat Samsung TV du propriétaire | Auteur Samsung + distributeur lié au DUID | idem (`--partner`) | Compte Samsung |
| Accès | **Root** (samsung-tv-root, firmware 1300.9) | Contexte SDK UID 901 via le transport samsung-tv-root, **sans root** | **`sdb shell` simple** | Aucun | Aucun |
| Home | 3 sélecteurs : handler `operation/main` + `home_appid` + `startup_appid` | Les 3 sélecteurs (helper `Routing.dll` exécuté par `/usr/bin/dotnet`) | `home_appid` → wrapper, `startup_appid` → launcher | Surveille l'API `:8001` et revient via l'AppControl `eden_resume` | Pas de routage : Home Samsung inchangé |
| Démarrage | OK après redémarrage complet | **Échec** (reprise HDMI) | `startup_appid` | Règles SmartThings cloud | *Autorun Last App* |
| Sauvegarde / restauration | Snapshots + `restore-launcher.sh` | `tv.py status/route/restore` + `routing-before.json` | `home-backup.txt` + `restore-home.sh` (bug probable du flag `--remove`) | Désinstallation + suppression des règles | Désinstallation |
| Testé sur | UN75U8000F, Tizen 9 | QN55QN90B, fw 1720.7 | UN85M70HB, fw oct. 2026 | UA43UE86AF, Tizen 9 | 2015+ (communauté) |
| Maturité | 408 ★, 1 TV | 0 ★, 1 TV | 0 ★, 1 TV | 2 ★, 1 TV | 4,9k ★, nombreuses TV |

**« Opt-quiet-tizen » : introuvable** (ni sur GitHub, ni sur le web). Si vous avez le lien, donnez-le-moi. Le fork le plus proche d'une version « optimisée » est quiet-tizen-qn90b.

### Ce qu'on réutilise, ce qu'on réécrit, ce qu'on écarte

| Élément | Décision | Raison |
|---|---|---|
| TPK autonome avec son propre package id (qn90b) | **Réutiliser le principe** | Aucun binaire Samsung touché, désinstallation propre |
| Routage par `vconftool` sur `sdb shell` (Fresh Home) | **Réutiliser** | Seule voie sans root ni exploit ; à valider sur votre firmware |
| Pattern *wrapper* pour Home (Fresh Home) | **Réutiliser** | Le firmware relance l'app Home avec `__NO_RAISE_WINDOW` : un launcher enregistré directement reste derrière l'écran de chargement. Une mini-app relais lance le vrai launcher puis se ferme. |
| Sauvegarde avec garde-fous et restauration ciblée par sélecteur (qn90b) | **Réécrire** en PowerShell avec la même logique | Vérifier avant d'écrire, relire après, annuler automatiquement en cas d'échec |
| Handler `operation/main` via un helper .NET (`Routing.dll`, `Probe.dll`) | **Écarter au départ** | Exige le transport samsung-tv-root et une API privée. Fresh Home s'en passe. On ne l'ajoute que si les clés vconf ne suffisent pas. |
| Remplacement de `bixby-promotion`, root, SQL sur `/opt/dbspace` (quiet-tizen) | **Écarter** | Invasif, dépend d'un firmware précis, contraire à « ne pas casser le shell système » |
| Assemblies de référence extraites de la TV | **Remplacer** par les paquets NuGet publics `Tizen.NET` alignés sur l'API de la TV | L'extraction passe par le root. Risque : une API NUI présente sur PC mais absente de la TV. On le couvre par un test au démarrage. |

## 3. Dépendances et certificats

| Élément | Exigence |
|---|---|
| PC | Tizen Studio + CLI, extensions **TV** et **Samsung Certificate**, .NET SDK 6 ou 8 (compile en `netcoreapp3.1` ou `net6.0` selon le runtime de la TV) |
| Framework cible | Doit correspondre **exactement** au runtime de la TV : .NET Core 3.1 sur les 2020-2022, .NET 6 sur les 2024-2025. À lire dans la fiche. |
| Paquets | `Tizen.NET` (NuGet) en version alignée sur `api-version` ; NUI uniquement, sans Xamarin.Forms |
| Certificat | Profil **Samsung** → TV → certificat d'auteur Samsung + distributeur **lié au DUID**. Refusés selon Fresh Home : paquet non signé, certificat SDK générique, auteur seul. **Les mises à jour exigent le même certificat d'auteur** : sauvegardez le `.p12`. |
| Déploiement | `tizen package -t tpk -s <profil> -- <fichier>.tpk`, puis `sdb -s <IP>:26101 install <tpk>`. **Attention : `sdb install` peut renvoyer 0 même en cas d'échec.** Vérifier l'installation en lançant l'app (`tizen run -p <appid>`) et en la cherchant dans la liste Apps de la TV. |
| Mode développeur | Apps → `12345` → On + IP du PC → **redémarrage complet**. Peut être désactivé par une mise à jour firmware. |

## 4. Mécanisme Home / startup

Trois sélecteurs, du moins au plus invasif :

| Sélecteur | Rôle | Accès | Notre usage |
|---|---|---|---|
| `db/appfw/home_appid` | App lancée par le bouton Home | `sdb shell "vconftool set -t string -f ..."` | **Oui**, pointé vers l'app *wrapper* |
| `db/appfw/startup_appid` | App au démarrage | idem | **Oui**, pointé vers le launcher. Résultat incertain : on teste et on se replie sur *Autorun Last App* au besoin. |
| Handler `http://tizen.org/appcontrol/operation/main` | Gestionnaire Home système | API privée, contexte SDK/root | **Non** (phase ultérieure seulement si nécessaire) |
| `db/appfw/booting_appid_backup`, `db/first_screen/type` | Observés par Fresh Home | lecture | **Sauvegardés, jamais modifiés** |

Valeurs d'origine observées ailleurs, **à ne pas supposer chez vous** : `com.samsung.tv.csfs` (Home), `org.tizen.tv-viewer` ou `com.samsung.tv.csfs` (startup).

## 5. Risques

| Risque | Probabilité | Impact | Mesure |
|---|---|---|---|
| `vconftool` refusé sur votre firmware | Moyenne | Bloquant pour le Home | Test en lecture d'abord, via `tv-fiche.ps1` |
| Le launcher plante, Home pointe vers une app morte | Moyenne en dev | Bouton Home inopérant | Le wrapper a son propre repli : si le lancement échoue, il ouvre `com.samsung.tv.csfs`. La restauration reste possible par sdb. |
| Mise à jour firmware qui rétablit le Home Samsung | Élevée à terme | Faible | Relancer la routine de routage |
| Mise à jour firmware qui coupe le mode dev **alors que** Home pointe vers notre app | Faible | Élevé : plus de sdb pour restaurer | Le wrapper garantit que Home fonctionne toujours. Réglages accessibles depuis la tuile Paramètres du launcher et le menu rapide. Dernier recours : reset factory, qui remet les vconf par défaut. |
| Désinstallation du launcher avant restauration | Faible | Home cassé | Le script de désinstallation **refuse** tant que `home_appid` pointe vers nous |
| API NUI absente sur la TV | Moyenne | Crash au lancement | Test de démarrage sur la TV avant tout routage |
| Perte du certificat d'auteur | Faible | Impossible de mettre à jour, il faut désinstaller et réinstaller | Sauvegarde du `.p12` |
| Écosystème jeune (dépôts de quelques jours, 1 TV chacun) | Certaine | Docs incomplètes ou fausses | On vérifie tout sur votre TV, rien n'est supposé |

## 6. Plan de rollback (cible)

0. **Avant tout** : `tv-fiche.ps1` sauvegarde les valeurs d'origine dans `.local/`. Gardez la première fiche.
1. **Rollback du routage** : remettre `home_appid` et `startup_appid` aux valeurs sauvegardées par `vconftool set`, relire, puis redémarrer la TV.
2. **Rollback de l'app** : désinstaller le TPK, **seulement après l'étape 1**.
3. **Rollback des réglages** : *Autorun Last App* et *Démarrer avec Smart Hub* remis à l'état d'origine.
4. **Sans sdb** (mode dev perdu) : réactiver le mode dev (`12345`), puis refaire l'étape 1.
5. **Dernier recours** : reset factory (Paramètres → Assistance → Diagnostic → Réinitialiser), qui restaure les clés vconf par défaut.

## 7. Prochaines étapes proposées

1. **Vous** : lancer `tv-fiche.ps1` et me transmettre le JSON, ou au minimum le modèle, le firmware et le résultat des `vconftool get`.
2. **Recherche** : finaliser la fiche (section 1) et figer le framework cible.
3. **Architecture + Dev** : projet .NET/NUI (launcher + wrapper Home + config), avec un *smoke test* sur la TV **avant** tout routage.
4. **Intégration Home** : `tv-home.ps1` avec les actions `backup`, `route`, `verify` et `restore`, et des garde-fous à chaque étape.
5. **QA** : plan de test complet, avec **répétition de la restauration** avant de déclarer le projet stable.

## Sources

- [jss8649/quiet-tizen](https://github.com/jss8649/quiet-tizen) : `docs/02-access.md`, `docs/04-launcher.md`, `docs/09-recovery.md`
- [gus-cardoso/quiet-tizen-qn90b](https://github.com/gus-cardoso/quiet-tizen-qn90b) : `docs/03-build-and-test.md`, `docs/04-home-and-recovery.md`, `tools/tv.py`
- [KSWORK2001/fresh-home](https://github.com/KSWORK2001/fresh-home) : `platforms/samsung-tizen/install.sh`, `restore-home.sh`
- [SaudSatopay/skylight-tizen](https://github.com/SaudSatopay/skylight-tizen)
- [simbasjungles/free-my-tv](https://github.com/simbasjungles/free-my-tv)
- [chris-ritsen/samsung-tv-root](https://github.com/chris-ritsen/samsung-tv-root) (lu seulement pour comprendre le transport utilisé par qn90b ; non retenu)
- [Samsung : Launch Your Tizen .NET Application on Samsung Smart TV](https://developer.samsung.com/tizen/blog/en/2019/02/19/launch-your-tizen-net-application-on-samsung-smart-tv)
- [Article « Clean Frame »](https://youmind.com/landing/x-viral-articles/samsung-frame-custom-home-screen) : Frame LS03D, routage Home sans root, code non publié
