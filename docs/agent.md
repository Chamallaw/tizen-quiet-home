# Installer et personnaliser avec un agent de code

[English](en/agent.md) · **Français**

Quiet Home a été développé avec un agent de code (Claude Code) qui pilotait le PC et la TV. Vous pouvez faire de même pour l'installer, l'adapter à votre TV ou le faire évoluer.

## Ce que l'agent fait, et ce qui reste à vous

| L'agent peut | Vous seul pouvez |
|---|---|
| Lire la fiche de la TV (`tools/tv-fiche.ps1`) | Activer le mode développeur sur la TV (télécommande) |
| Installer le .NET SDK en local, compiler, tester | Installer les extensions Tizen (droits administrateur) |
| Signer, installer, lancer l'app (`sdb`, Tizen CLI) | Créer le certificat Samsung (connexion à votre compte) |
| Écouter les logs et diagnostiquer | Ouvrir le port des logs dans le pare-feu (réglage de sécurité du PC) |
| Ajuster favoris, consoles, logos, thèmes | Appuyer sur les boutons de la télécommande pendant les tests |
| Générer des rendus de l'interface | Regarder l'écran : l'agent ne voit que les logs et les rendus |

## Prompt de départ

Ouvrez le dépôt dans votre agent, puis collez :

```text
Lis AGENTS.md puis docs/install.md. Ma TV Samsung est en mode développeur à l'adresse <IP_DE_LA_TV>,
avec l'IP de ce PC déclarée. Objectif : installer Quiet Home sur cette TV.
1. Établis la fiche de la TV (tools/tv-fiche.ps1) et dis-moi si le modèle est compatible.
2. Vérifie les outils (Tizen Studio, .NET 8, Python) et installe ce qui manque sans droits admin.
3. Arrête-toi et guide-moi pour le certificat Samsung et le pare-feu.
4. Lance les tests, déploie, écoute les logs et confirme le démarrage.
5. Adapte les favoris aux apps réellement installées (Diagnostic ou logs), et les consoles à mes entrées HDMI.
Ne modifie aucun réglage système de la TV ni du PC sans me le demander.
```

## Prompts utiles ensuite

- *« Ajoute <app> aux favoris, avec son logo officiel s'il existe dans Simple Icons. »*
- *« Ma PS5 est sur HDMI 3 : calibre la carte avec Diagnostic → Tester HDMI et les logs. »*
- *« Fais-moi un rendu de l'accueil dans le thème Chêne fumé. »*
- *« Le bouton Home ne revient pas sur Quiet Home : regarde les lignes "premier plan →" dans les logs. »*

## Conseils

- **Logs d'abord** : demandez à l'agent d'écouter les logs (`tv-native.ps1 -Action logs`, ou `native/tools/logs_listen.py` en arrière-plan) avant chaque test. Sans eux, il travaille à l'aveugle.
- **Rendus** : l'agent ne voit pas l'écran. `make_preview_kuri.py` (avec `en` pour l'anglais) lui permet de vous montrer l'interface, à partir des mêmes valeurs que l'app.
- **Vos données** : la fiche de la TV et les logs restent dans `.local/` (ignoré par git). Ne publiez ni le DUID, ni les certificats, ni vos IP.
