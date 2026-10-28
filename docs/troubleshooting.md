# Dépannage et retour à l'état d'origine

[English](en/troubleshooting.md) · **Français**

## Problèmes fréquents

| Symptôme | Cause probable | Solution |
|---|---|---|
| `sdb devices` vide | TV en veille, IP changée ou mode développeur réinitialisé | Allumez la TV, `sdb connect <IP>`, vérifiez l'IP du PC dans le mode développeur |
| Certificate Manager : page de connexion blanche | Windows Server, sans Internet Explorer | Créez le profil sur Windows 10 ou 11 et copiez-le |
| `Le paquet n'est pas signé` | Profil introuvable, ou mauvais nom passé à `-CertProfile` | Vérifiez `C:\tizen-studio-data\profile\profiles.xml` |
| Installation refusée | Certificat distributeur sans le DUID de la TV, ou autre certificat d'auteur | Recréez le distributeur avec le bon DUID ; gardez le même certificat d'auteur pour les mises à jour |
| Aucun log sur le PC | Pare-feu Windows, ou mode développeur inactif | Règle UDP 5140 (voir [install.md](install.md)) ; mode développeur pointé vers ce PC |
| Home ouvre l'accueil Samsung | Quiet Home fermé, ou raccourci désactivé | Relancez Quiet Home ; Réglages → *Bouton Home* |
| Carte HDMI : « refusée » ou « non disponible » | Identifiant de source différent sur votre modèle | Diagnostic → *Tester HDMI*, puis ajustez `InputSpec` |
| Logo absent | Marque sans logo libre | Normal : la tuile garde la couleur de la marque et affiche le nom |
| Interface dans la mauvaise langue | *Automatique* suit la langue de la TV | Réglages → *Langue* (*Language* en anglais) : choisissez Français ou English |

Le panneau **Réglages → Récupération et diagnostic → Diagnostic** affiche le chemin de la configuration, la langue utilisée, toutes les apps installées et le journal, même sans PC.

## Retour à l'état d'origine (du plus léger au plus complet)

1. **Home deux fois** : l'accueil Samsung s'affiche. Rien n'a été modifié côté Samsung.
2. **Réglages → Bouton Home**, valeur *Accueil Samsung* : le raccourci est désactivé.
3. **Récupération → Réinitialiser la configuration** : les favoris et le thème reviennent à leurs valeurs d'origine.
4. **Désinstaller l'app** : `tv-native.ps1 -Action uninstall`, ou depuis la barre Apps.
5. **Réglages TV** : *Autorun Last App* et *Exécution auto Smart Hub* reviennent à leur état d'origine.
6. **Quitter le mode développeur** : Apps → `12345` → Off, puis redémarrez la TV.

Un reset usine n'est jamais nécessaire : Quiet Home ne modifie rien en dehors de son propre dossier.

## Image un peu floue

La TV fait dessiner toutes les apps en 1920 × 1080, même sur une dalle 4K ou 8K, puis agrandit l'image. Le log de démarrage l'indique (« fenêtre 1920x1080 … écran annoncé 1920x1080 »). Quiet Home dessine ses logos, pictogrammes et textes au pixel près pour ce 1080p. Un léger adoucissement dû à l'agrandissement reste inévitable. Le réglage *Netteté* de l'image de la TV l'atténue un peu.
