# Quiet Home natif (.NET / NUI)

Launcher natif, développé et testé sur une **Samsung QE65QN700BTXXC** (2022) : Tizen 6.5 (API 9), armv7, runtime .NET Core 3.1 embarqué. C'est une application `.tpk` ordinaire : on l'installe et on la désinstalle comme n'importe quelle app. Le système Samsung n'est pas modifié.

- **Démarrage** : réglage Samsung *Autorun Last App*.
- **Bouton Home** : il ramène à Quiet Home ; **deux appuis rapides** laissent l'accueil Samsung (Smart Hub et paramètres natifs). Le système n'est pas modifié : l'app observe l'app au premier plan (`ApplicationManager`) et revient dès que l'accueil Samsung (`com.samsung.tv.csfs`) s'affiche. Les changements de source (`org.tizen.tv-viewer`) ne sont jamais interceptés. Désactivable dans Réglages → « Bouton Home ».

## TV de référence

| | |
|---|---|
| Modèle | QE65QN700BTXXC (`22_OSCARS_8K`), dalle 7680x4320 |
| Tizen | 6.5 (`platform_version`), API 9, profil `tv-samsung` |
| CPU | armv7, 32 bits |
| sdb | `sdbd_rootperm:disabled`. `shell` fermé (même `echo` est refusé) ; seules les commandes `0 …` répondent. |
| Débogage .NET | `netcoredbg_support:enabled` |

## Architecture

```
native/
├── src/QuietHome.Core/          netstandard2.0, logique pure, testée sur PC
│   ├── Model.cs                 AppEntry, Tile, Row, LauncherConfig (favoris, masqués, entrées, thème…)
│   ├── DefaultConfig.cs         favoris calibrés sur les 35 apps de votre TV
│   ├── ConfigStore.cs           JSON atomique (.tmp → remplacement, .bak), repli sur les défauts
│   ├── Catalog.cs               rangées Favoris / Entrées / Applications / Réglages rapides
│   ├── FocusModel.cs            navigation télécommande, colonne mémorisée par rangée
│   └── Themes.cs                Japandi (clair), Sumi (sombre), Hinoki (bois) + tons des tuiles
├── src/QuietHome.Launcher/      netcoreapp3.1 + Tizen.NET 9 (non embarqué)
│   ├── tizen-manifest.xml       QHLauncher / QHLauncher.Home, 4 privilèges publics
│   ├── Program.cs               cycle de vie NUIApplication, filet « retour Samsung » si plantage au démarrage
│   ├── Platform/AppService.cs   liste (ApplicationManager) + lancement (AppControl)
│   ├── Platform/TvControls.cs   volume / muet / HDMI via les bibliothèques natives Samsung, échec propre
│   ├── Platform/Log.cs          tampon visible dans Diagnostic + UDP vers le PC
│   └── UI/                      HomeScreen, TileView, OverlayMenu, Palette
├── tests/QuietHome.Core.Tests/  xUnit (16 tests)
└── tools/
    ├── tv-native.ps1            test | build | sign | deploy | install | run | logs | uninstall
    └── pack_tpk.py              assemblage du .tpk
```

**Interface** (1920x1080 logique, mise à l'échelle de la fenêtre), direction **Japandi** :
- neutres chauds, beaucoup d'air, typographie légère ;
- grande horloge fine et date, en français ou en anglais (`Lang`) ;
- **Favoris** : Netflix, Jellyfin, Spotify, Prime Video, Disney+, en cartes à dégradé sourd (brique, glycine, mousse, ardoise, encre) ;
- **Consoles** : PS5 (HDMI 2) et Switch (HDMI 4) ;
- une ligne de puces : Toutes les apps, Volume −/+, Muet, Ambiance, Paramètres, **Récupération**.

**Logos et couleurs officiels** :
- `tools/fetch_marks.py` télécharge les logos depuis les sources épinglées utilisées par NelsonEU/arn0-tv : Simple Icons v13.21.0 (CC0) et Custom Brand Icons (MIT) pour Disney+. La provenance de chaque fichier est listée dans `res/marks-src/SOURCES.md`.
- `tools/render_marks.py` les convertit en PNG blancs recadrés dans `res/icons/`. L'app les teinte à l'exécution : logo rouge sur noir pour Netflix, vert sur noir pour Spotify, blanc sur le dégradé de la marque pour les autres.
- Les noms et logos appartiennent à leurs propriétaires ; ils servent ici à un usage personnel.
- `tools/make_preview.py` produit une maquette HTML 1920x1080 de l'accueil, à exporter en PNG avec Chrome headless.

Le focus agrandit légèrement la carte et affiche un trait discret dessous. « Toutes les apps » liste les apps lançables, et **Gérer les favoris…** permet d'épingler à la télécommande, sans touches de couleur. La configuration v1 est migrée automatiquement vers ces défauts.

Le panneau **Récupération**, toujours accessible, propose :
- ouvrir l'accueil Samsung ;
- ouvrir les paramètres TV ou réseau ;
- réafficher les apps masquées ;
- rafraîchir la liste des apps ;
- réinitialiser la configuration ;
- le diagnostic : tous les ID d'apps et le journal ;
- le mode d'emploi du démarrage automatique ;
- fermer l'app.

| Télécommande | Action |
|---|---|
| Flèches / OK | Naviguer / ouvrir |
| OK maintenu (0,6 s) | Options de la tuile : favori, déplacer, masquer |
| Retour | Revenir au programme TV (`org.tizen.tv-viewer`) |
| Jaune / Bleu / Vert / Rouge | Raccourcis facultatifs : Épingler / Masquer / Déplacer / Rafraîchir |
| Home | Retour à Quiet Home (≈ 1 s) ; deux appuis rapides = accueil Samsung |

**Points à valider sur la TV** : ils sont codés pour échouer proprement, avec un message à l'écran.
- **Entrées HDMI** : `libsource-api.so` / `connect_source` et les ID 13 à 16 viennent d'un U8000F. Ils peuvent différer sur votre modèle, ou être refusés pour une app signée développeur.
- **Volume / muet** : `libcapi-media-sound-manager-tv.so.0`, avec la même réserve.
- **Paramètres** : `org.tizen.menu`, avec l'extra `menuitem` pour les sous-menus.
- **Icônes** : celles des autres apps peuvent être illisibles. Une pastille à initiales s'affiche alors.

## 1. Préparer le PC (une seule fois, à faire par vous)

Le gestionnaire de paquets Tizen demande les droits administrateur, et le certificat demande votre compte Samsung.

1. Lancez `C:\tizen-studio\package-manager\package-manager.exe` **en administrateur**.
2. Dans *Extension SDK*, installez **Samsung Certificate Extension** et **TV Extensions-6.5** (ou la plus proche).
3. Ouvrez le *Certificate Manager* (`C:\tizen-studio\tools\certificate-manager\certificate-manager.exe`), cliquez sur **+**, puis :
   - choisissez **Samsung**, puis **TV** ;
   - nommez le profil **`QuietHomeTV`** ;
   - créez le certificat d'auteur et connectez-vous à votre compte Samsung ;
   - choisissez le niveau de privilège distributeur **Public** et vérifiez que le DUID de votre TV est bien dans la liste (détecté automatiquement si la TV est connectée par `sdb` ; sinon `sdb shell 0 getduid`).
4. **Sauvegardez** `C:\tizen-studio-data\keystore\` : les mises à jour de l'app exigent le même certificat d'auteur.
5. Dans le *Device Manager* de Tizen Studio, faites un clic droit sur la TV puis **Permit to install applications**.

Le SDK .NET 8 peut être installé en local (`dotnet-install.ps1 -InstallDir "$env:USERPROFILE\.dotnet"`) : `tv-native.ps1` le trouve automatiquement.

## 2. Construire, signer, installer

```powershell
.\native\tools\tv-native.ps1 -Action test
```

```powershell
.\native\tools\tv-native.ps1 -Action deploy -CertProfile QuietHomeTV
```

`deploy` enchaîne les étapes suivantes :
1. `dotnet build` ;
2. `dist\QHLauncher-0.1.0-unsigned.tpk` ;
3. `tizen package -t tpk -s QuietHomeTV`, puis vérification que `author-signature.xml` et `signature1.xml` sont présents ;
4. `sdb install`, avec contrôle du message de la TV ;
5. `tizen run -p QHLauncher.Home`.

**Mise à jour** : incrémentez `-Version` (ex. `-Version 0.1.1`) et relancez `deploy`. La configuration est conservée dans le dossier `data` de l'app.

## 3. Démarrage automatique

1. Ouvrez *Paramètres → Général → Fonctions Smart*.
2. Activez **Exéc. auto dernière app** (*Autorun Last App*).
3. Désactivez **Exécution auto Smart Hub**.
4. Ouvrez Quiet Home, puis éteignez la TV.
5. Épinglez Quiet Home en tête de la barre Apps : il reste alors à un clic après un appui sur Home.

## 4. Débogage

| Besoin | Méthode |
|---|---|
| Logs en direct | `.\native\tools\tv-native.ps1 -Action logs`. L'app envoie ses logs en UDP au PC déclaré dans le mode développeur de la TV, port 5140 (ou à `DevLogHost` s'il est renseigné). Si rien n'arrive, autorisez UDP 5140 entrant sur le profil réseau *Privé* du pare-feu Windows. |
| Sur la TV | Réglages rapides → Récupération → **Diagnostic** : chemin de la config, taille de la fenêtre, tous les ID d'apps, les 200 dernières lignes du journal |
| Calibrer les HDMI | Essayez une carte Entrée ; le message (`Source 14 refusée (-2)`, `Non disponible…`) part dans les logs. Ajustez `Inputs` dans la config. |
| Débogueur | `netcoredbg` est supporté par la TV : extension Tizen pour VS Code ou Visual Studio, *Attach* via le Device Manager |
| L'app ne démarre pas | Le filet de `Program.cs` relance l'accueil Samsung. Vérifiez la signature (`-Action sign`) et le message de `sdb install`. |

## 5. Rollback

1. **Immédiat** : bouton **Home**, qui ouvre l'accueil Samsung, jamais modifié.
2. **Configuration** : Récupération → *Réinitialiser la configuration*.
3. **Démarrage** : remettez *Autorun Last App* et *Exécution auto Smart Hub* à leur état d'origine.
4. **Désinstallation** : `.\native\tools\tv-native.ps1 -Action uninstall`, ou depuis la barre Apps. La TV revient exactement à son état d'avant.
5. **Mode développeur** : Apps → `12345` → Off → redémarrage.

## 6. Plan de tests sur la TV

**A. Installation**
- [ ] `-Action test` : 14/14
- [ ] `-Action sign` : signatures présentes
- [ ] `sdb install` sans message d'erreur, l'app apparaît dans la barre Apps
- [ ] Premier lancement en **moins de 3 s** ; logs `démarrage QHLauncher.Home` reçus

**B. Interface et télécommande**
- [ ] Navigation dans les 4 directions sans perte de focus, défilement horizontal au-delà de 6 tuiles
- [ ] OK maintenu → options ; épingler, masquer et déplacer persistent après relance
- [ ] Retour → programme TV ; relance depuis la barre Apps → focus remis au début
- [ ] Ambiance ×3 (Japandi, Sumi, Hinoki) : dégradés nets, sans banding, lisibles sur la dalle 8K
- [ ] Toutes les apps → Gérer les favoris → l'épinglage apparaît dans la rangée Favoris

**C. Lancement d'apps**
- [ ] Netflix, Prime Video, Disney+, TizenTube, Plex, Jellyfin, CANAL+, france.tv
- [ ] Retour d'une app vers Quiet Home (bouton Retour ou Apps) → liste rafraîchie
- [ ] App non installée → tuile grisée + message

**D. Fonctions TV** (à calibrer)
- [ ] HDMI 1 à 4 : notez le message de chaque carte
- [ ] Volume −/+ et Muet : notez le message
- [ ] Paramètres TV et Paramètres réseau s'ouvrent

**E. Cycle de vie**
- [ ] Démarrage à froid (débranchement 1 min) avec Autorun Last App → Quiet Home s'ouvre
- [ ] Veille puis reprise → Quiet Home ou dernière app, sans écran noir
- [ ] 20 allers-retours app ↔ Quiet Home sans ralentissement
- [ ] Home depuis une app → Quiet Home ; Home deux fois → Smart Hub ; paramètres natifs accessibles
- [ ] Changement de source (PS5, Switch) : Quiet Home ne s'interpose pas
- [ ] Réglages → « Bouton Home : accueil Samsung » désactive l'interception

**F. Rollback (obligatoire avant de déclarer stable)**
- [ ] Réinitialiser la configuration → favoris d'usine
- [ ] Désinstallation → plus aucune trace, la TV démarre comme avant
- [ ] Réinstallation avec le même certificat → OK
