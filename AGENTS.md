# Consignes pour les agents de code

Ce dépôt construit **Quiet Home**, un launcher natif .NET/NUI (`.tpk`) pour TV Samsung Tizen. Lisez ce fichier avant d'agir sur la TV ou le PC de l'utilisateur.

## Règles

1. **Pas de modification du système.** Pas de root, pas d'exploit, pas d'écriture de clés vconf ni de bases système, pas de remplacement d'apps Samsung. Quiet Home reste une app ordinaire, entièrement réversible par désinstallation.
2. **Les réglages de sécurité restent à l'utilisateur** : pare-feu, compte Samsung, certificats, droits administrateur. Donnez-lui la commande exacte et attendez sa confirmation.
3. **Ne publiez pas de données personnelles** : DUID, MAC, IP, certificats (`%USERPROFILE%\SamsungCertificate`), contenu de `.local/`.
4. **Vérifiez sur la TV** avec les logs (UDP 5140) plutôt que de supposer. L'agent ne voit pas l'écran : demandez à l'utilisateur d'appuyer sur les touches et de décrire ce qu'il voit, ou générez un rendu.

## Faits établis (Tizen 6.5, QN700B 2022)

- `sdb shell` est **fermé** : même `echo` ne renvoie rien. Seules les commandes `0 …` du plugin Samsung répondent (`sdb shell 0 getduid`). Inutile d'essayer `vconftool`.
- `sdb install` peut sortir en 0 en cas d'échec : lisez sa sortie (`install completed`).
- Le runtime de la TV est **.NET Core 3.1**. On compile en `netcoreapp3.1` contre le paquet NuGet `Tizen.NET` 9.0.0.16760, sans l'embarquer (`ExcludeAssets=runtime`).
- `GetRunningApplicationsAsync()` fonctionne en arrière-plan et donne l'app au premier plan : `com.samsung.tv.csfs` pour l'accueil Samsung, `org.tizen.tv-viewer` pour une source HDMI.
- En arrière-plan, utilisez un **`System.Threading.Timer`** pour tout travail périodique avec `await` : les continuations sur la boucle NUI ne reprennent pas quand l'app est en pause.
- L'API locale `http://127.0.0.1:8001/api/v2/` répond sans jeton, depuis la TV comme depuis le LAN, en lecture seule. Elle indique `developerIP` et `developerMode`, et l'état des apps de la boutique, référencées par leur identifiant boutique.
- Le Certificate Manager de Tizen Studio repose sur Internet Explorer : sa page de connexion reste vide sur Windows Server.

## Commandes

```powershell
.\native\tools\tv-native.ps1 -Action test
.\native\tools\tv-native.ps1 -TvIp <IP> -Action deploy -CertProfile <PROFIL> -Version <x.y.z>
.\native\tools\tv-native.ps1 -Action logs
python -I native\tools\logs_listen.py <fichier> <minutes>
.\tools\tv-fiche.ps1 -TvIp <IP>
```

## Conventions du code

- La logique testable va dans `QuietHome.Core` (netstandard2.0, sans Tizen), avec un test xUnit. Le code Tizen et l'interface vont dans `QuietHome.Launcher`.
- Tout changement des valeurs par défaut qui doit toucher les configurations déjà enregistrées passe par une **migration** : incrémentez `LauncherConfig.CurrentVersion` et ajoutez une branche dans `ConfigStore.Normalize`.
- Interface en coordonnées logiques 1920x1080, positionnement absolu, mise à l'échelle de la fenêtre.
- Commentaires et textes de l'interface en français.
