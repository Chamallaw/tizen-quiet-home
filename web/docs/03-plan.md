# 3. Plan de projet

| Étape | Contenu | Critère de sortie | État |
|---|---|---|---|
| 0. Cadrage | Analyse de quiet-tizen, choix « Web App sans root » | Ce dossier `docs/` | ✅ |
| 1. Socle | App web modulaire, mock PC, tests Node | `node --test web/tests/*.test.js` au vert, UI navigable dans Chrome | ✅ |
| 2. Outillage | Tizen Studio + extension TV, certificat Samsung (DUID de la TV), `web/tv.ps1` | `tv.ps1 -Action connect` OK | ⬜ chez vous |
| 3. Premier déploiement | Installation sur la TV, test de lancement des apps | Checklist [05](05-checklist-tests.md), sections A et B | ⬜ |
| 4. Démarrage auto | Réglages natifs : Autorun Last App ON, Autorun Smart Hub OFF | La TV s'allume directement sur Quiet Home | ⬜ |
| 5. Ajustements | Corriger les IDs des favoris selon *votre* modèle (`defaults.js`), filtrer les apps système visibles | Plus aucun favori grisé à tort | ⬜ |
| 6. Stabilisation | 1 semaine d'usage normal, relevé des anomalies | Checklist [05](05-checklist-tests.md), section D | ⬜ |
| 7. (Optionnel) | Étendre : rangée « Récents », thème, réglages en UI ; **ou** évaluer la voie root après validation | Décision documentée | ⬜ |

Rôles (repris de votre organisation) :
- **Recherche** → [01-analyse.md](01-analyse.md)
- **Architecture** → [02-architecture.md](02-architecture.md)
- **Développement** → `src/`, `web/tests/`
- **Tests** → [04-installation-debug-rollback.md](04-installation-debug-rollback.md), [05-checklist-tests.md](05-checklist-tests.md)
- **Sécurité / Compatibilité** → [06-securite-compatibilite.md](06-securite-compatibilite.md)
