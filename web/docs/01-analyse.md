# 1. Analyse de quiet-tizen et des solutions existantes

## quiet-tizen (jss8649/quiet-tizen) : ce que c'est vraiment

| Point | Constat |
|---|---|
| Nature | **Étude de cas** sur **une seule TV** (UN75U8000FFXZA, 2025, Tizen 9.0, firmware `1300.9`). L'auteur la décrit comme une boîte à outils, pas comme un installeur universel. |
| Lanceur | « Quiet Home », app **native .NET/NUI** (`launcher/src/Home.cs`) : cartes HDMI, bibliothèque d'apps, réglages rapides, appairage Bluetooth. |
| Remplacement du Home | Obtenu en **réécrivant des préférences système**, ce qui demande un **accès root** (`tools/tv.py`, route « samsung-tv-root »). |
| Nettoyage | Profils SQL (`profiles/*.json`, `tools/plan_cleanup.py`) qui désactivent pubs, voix, TV Plus, etc. dans les bases internes de la TV. |
| Build | .NET 6 + **quatre assemblies Tizen privées non redistribuées**. Le lanceur n'est pas compilé en CI. |
| Fiabilité | Seule une partie du nettoyage survit au redémarrage. Les mises à jour d'apps préchargées de Samsung peuvent **rétablir le Home d'origine**. Le projet n'a jamais été rejoué sur une deuxième TV. |
| Licence | MIT pour le code original, licences tierces séparées. |

### Limites pour votre projet

1. **Root obligatoire** pour la partie « remplacer le Home ». Ce root passe par une faille propre à un firmware : le risque de blocage est réel, et une mise à jour firmware peut tout casser ou combler la faille.
2. **Compatibilité nulle par défaut**. Le README insiste : même marque, même année et même version de Tizen ne suffisent pas.
3. **Toolchain non reproductible** (assemblies privées).
4. Les modifications SQL des bases système sont **difficiles à vérifier** et le rollback dépend de snapshots pris au bon moment.

**Conclusion** : bonne source d'inspiration (UX, liste des nuisances à couper, procédure de snapshot/restauration), mais **pas une base sur laquelle construire** si l'objectif est un système stable, réversible et documenté.

## Autres solutions existantes

| Approche | Root ? | Remplace vraiment le Home ? | Réversibilité | Commentaire |
|---|---|---|---|---|
| **Tizen Web App sideloadée (mode dev)**, notre choix | Non | Non, mais elle s'ouvre au démarrage via « Autorun Last App » | Totale (désinstallation) | API publiques, aucun risque pour le firmware |
| Lanceur natif .NET (style quiet-tizen) | Oui pour le Home | Oui | Moyenne | Toolchain privée, fragile |
| Réglages Samsung natifs | Non | Non | Totale | « Autorun Smart Hub » OFF, désactivation des recommandations, pubs, etc. Gain réel et gratuit |
| Box externe (Android TV, Apple TV…) | n/a | Contourne Tizen | Totale | Hors périmètre, mais c'est la solution la plus robuste |
| Root + modification système | Oui | Oui | Faible | À réserver à l'expérimentation pure, après reset factory possible |

## Faits techniques Tizen retenus

- Une app web tierce **ne peut pas se déclarer launcher** : le Home Samsung reste le point d'entrée du système. Aucune API publique ne le permet.
- `tizen.application.getAppsInfo()` liste les apps installées. `tizen.application.launch(appId)` les lance (privilège **public** `application.launch`).
- `tizen.tvinputdevice.registerKey()` permet de capter les touches de couleur de la télécommande.
- Le changement de source HDMI depuis une app (`tvwindow` / `tvinputsource`) exige des privilèges **partner**, donc un certificat Samsung partenaire. Ce n'est pas faisable sur une TV grand public : on garde la touche **SOURCE** native.
- En mode développeur, on installe un `.wgt` signé avec un **certificat Samsung** (profil TV, DUID de la TV) via `sdb` + Tizen CLI.
