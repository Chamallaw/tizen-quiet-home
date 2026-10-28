# Installation pas à pas

[English](en/install.md) · **Français**

Comptez environ 30 minutes la première fois. Ensuite, une mise à jour se fait en une commande.

## 0. Ce qu'il faut

- Une **TV Samsung Tizen** sur le même réseau local que le PC.
- Un **PC Windows**, de préférence **Windows 10 ou 11**. Sur Windows Server, la page de connexion du Certificate Manager reste blanche (voir l'étape 3).
- Un **compte Samsung**, pour le certificat.

## 1. Installer les outils sur le PC

1. [**Tizen Studio**](https://developer.tizen.org/development/tizen-studio/download) (version CLI ou complète), dans `C:\tizen-studio`.
2. Lancez `C:\tizen-studio\package-manager\package-manager.exe` **en administrateur** :
   - onglet *Main SDK* : **Certificate Manager** (souvent déjà installé) ;
   - onglet *Extension SDK* : **Samsung Certificate Extension** (indispensable). *TV Extensions* est facultatif.
   - Les plateformes mobile ou montre et l'émulateur sont inutiles.
3. **.NET 8 SDK**, par exemple en local, sans droits administrateur :

   ```powershell
   Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1; .\dotnet-install.ps1 -Channel 8.0 -InstallDir "$env:USERPROFILE\.dotnet"
   ```

4. **Python 3**, pour l'assemblage du paquet.

## 2. Passer la TV en mode développeur

1. Notez l'IP de la TV : *Paramètres → Général → Réseau → État du réseau*.
2. Ouvrez **Apps**, tapez **1 2 3 4 5** à la télécommande, mettez *Developer mode* sur **On** et saisissez **l'IP du PC**.
3. Éteignez complètement la TV, en la débranchant ou en maintenant le bouton marche, puis rallumez-la.
4. Vérifiez la connexion depuis le PC :

   ```powershell
   C:\tizen-studio\tools\sdb.exe connect <IP_DE_LA_TV>
   ```

   La TV doit apparaître dans `C:\tizen-studio\tools\sdb.exe devices`.

5. *Optionnel* : `.\tools\tv-fiche.ps1 -TvIp <IP_DE_LA_TV>` écrit dans `.local/` la fiche de la TV (modèle, version de Tizen, architecture). Ce script ne fait que lire.

## 3. Créer le certificat Samsung

1. Ouvrez `C:\tizen-studio\tools\certificate-manager\certificate-manager.exe`.
2. Cliquez sur **+**, choisissez **Samsung**, puis **TV**, et donnez un nom au profil, par exemple `QuietHomeTV`.
3. Créez le **certificat d'auteur** en vous connectant à votre compte Samsung.
4. Créez le **certificat distributeur** :
   - niveau **Public** ;
   - le **DUID de la TV** doit figurer dans la liste. Il est détecté si la TV est connectée par `sdb` ; sinon, ajoutez-le à la main (`sdb shell 0 getduid`).
5. **Sauvegardez** `%USERPROFILE%\SamsungCertificate\<profil>\` : les mises à jour de l'app exigent le même certificat d'auteur.

> **Page de connexion vide ?** Le navigateur intégré au Certificate Manager repose sur Internet Explorer, absent de Windows Server. Créez le certificat sur un PC Windows 10 ou 11, puis copiez le dossier du profil.

> **La TV n'apparaît pas dans la liste ?** Elle s'est déconnectée, par exemple après une mise en veille. Relancez `sdb connect <IP>`, revenez d'une étape, ou saisissez le DUID à la main.

## 4. Construire, signer, installer

```powershell
.\native\tools\tv-native.ps1 -Action test
```

```powershell
.\native\tools\tv-native.ps1 -TvIp <IP_DE_LA_TV> -Action deploy -CertProfile QuietHomeTV
```

`deploy` enchaîne :
1. `dotnet build` ;
2. l'assemblage du `.tpk` ;
3. la signature, puis la vérification que le paquet est bien signé ;
4. `sdb install`, avec contrôle du message renvoyé par la TV ;
5. le lancement de l'app.

Pour **mettre à jour**, relancez `deploy` avec un nouveau numéro de version, par exemple `-Version 1.0.1`. La configuration est conservée.

## 5. Démarrer sur Quiet Home

- **À l'allumage** : *Paramètres → Général → Fonctions Smart* :
  - activez **Exéc. auto dernière app** (*Autorun Last App*) ;
  - désactivez **Exécution auto Smart Hub** ;
  - ouvrez Quiet Home, puis éteignez la TV.
- **Bouton Home** : rien à faire, Quiet Home s'en occupe dès qu'il tourne. Voir [home-button.md](home-button.md).
- **Langue** : Quiet Home suit la langue de la TV (français si la TV est en français, anglais sinon). Pour la choisir : Réglages → *Langue*.

## 6. Voir les logs (recommandé pendant les essais)

L'app envoie ses logs en UDP au PC déclaré dans le mode développeur, sur le port 5140. Autorisez ce port **depuis la TV uniquement**, dans un PowerShell lancé en administrateur :

```powershell
New-NetFirewallRule -DisplayName "Quiet Home logs (TV)" -Direction Inbound -Protocol UDP -LocalPort 5140 -RemoteAddress <IP_DE_LA_TV> -Action Allow
```

Puis lancez l'écoute :

```powershell
.\native\tools\tv-native.ps1 -Action logs
```

Pour retirer la règle plus tard : `Remove-NetFirewallRule -DisplayName "Quiet Home logs (TV)"`.

## 7. Désinstaller

```powershell
.\native\tools\tv-native.ps1 -TvIp <IP_DE_LA_TV> -Action uninstall
```

Vous pouvez aussi désinstaller depuis la barre Apps de la TV. Ensuite, remettez *Autorun Last App* et *Exécution auto Smart Hub* à leur état d'origine. Rien d'autre n'a été modifié sur la TV.
