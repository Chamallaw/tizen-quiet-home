# Le raccourci du bouton Home

## Comportement

| Geste | Résultat |
|---|---|
| **Home** depuis une app (Netflix, YouTube, Jellyfin…) | Retour à Quiet Home en moins d'une seconde, sélection sur la tuile de cette app. La barre Samsung apparaît un instant. |
| **Home deux fois** : le second appui moins de 6 s après le retour | L'accueil Samsung reste affiché (Smart Hub, paramètres natifs). |
| **Source** → HDMI (PS5, Switch…) | Jamais intercepté. |
| Réglages → *Accueil Samsung* (ou Récupération → *Ouvrir l'accueil Samsung*) | Jamais intercepté. |
| Réglages → *Bouton Home*, valeur *Accueil Samsung* | Désactive complètement le raccourci et le retour au rallumage. Ce choix est mémorisé. |
| **Rallumage de la TV** | Retour à Quiet Home si la TV reprend la dernière source, l'accueil Samsung ou l'app d'avant la veille. Un allumage direct sur une app (touche Netflix, Prime…) est respecté. |

### Au rallumage

En veille rapide, la TV garde Quiet Home en mémoire, mais au rallumage elle reprend la dernière **source** (souvent Samsung TV Plus). Quiet Home détecte la mise en veille de trois façons (touche Power, écran vide plus de 2 s, ou surveillance figée plus de 60 s) et revient dès qu'une app réapparaît :

```
17:30:40 touche Power : mise en veille
17:30:59 premier plan → org.tizen.tv-viewer
17:31:00 retour à Quiet Home (bouton Home ou rallumage)
17:31:07 premier plan → QHLauncher.Home
```

La TV met quelques secondes à se réveiller avant d'afficher Quiet Home : la source reprise reste visible pendant ce temps. Pour l'atténuer, dans *Paramètres → Général → Fonctions Smart* :
- désactivez **Exécution auto Samsung TV Plus** ;
- activez **Exéc. auto dernière app**.

Après une coupure de courant complète, Quiet Home n'est plus en mémoire : seul *Exéc. auto dernière app* peut le relancer.

## Comment ça marche, sans toucher au système

Une app tierce ne reçoit pas la touche Home : la TV la traite avant. Il n'est pas non plus possible de changer l'app d'accueil sans modifier des réglages système, et le shell `sdb` des TV récentes est de toute façon fermé.

Quiet Home **observe** donc l'app au premier plan, grâce à l'API publique `ApplicationManager.GetRunningApplicationsAsync()` interrogée chaque seconde, même quand Quiet Home est en arrière-plan. Les logs montrent un signal net :

| Ce que fait l'utilisateur | App au premier plan |
|---|---|
| Regarde Netflix | `org.tizen.netflix-app` |
| Appuie sur **Home** | `com.samsung.tv.csfs` (l'accueil Samsung) |
| Passe sur une console (**Source**) | `org.tizen.tv-viewer` |

Quand l'accueil Samsung (`com.samsung.tv.csfs`) passe au premier plan, Quiet Home **se relance lui-même** avec une requête AppControl ordinaire. Il ne ferme, ne tue et ne modifie aucune autre app.

Extrait de logs réel :

```
16:19:07.948 premier plan → org.tizen.netflix-app
16:19:27.179 premier plan → com.samsung.tv.csfs
16:19:27.383 bouton Home : retour à Quiet Home
16:19:28.051 premier plan → QHLauncher.Home
```

La logique de décision (`native/src/QuietHome.Core/HomeRedirect.cs`) est une machine à états pure, testée sur PC (`HomeRedirectTests.cs`) :
- **double appui** dans les 6 s qui suivent un retour : l'accueil Samsung est autorisé jusqu'à ce qu'une autre app passe au premier plan ;
- **relevés pendant la transition** : tant que Quiet Home n'est pas encore revenu, on attend au lieu de croire à un second appui, puis on relance une fois au bout de 3 s ;
- **autorisation explicite** (Récupération) : elle tient jusqu'à ce que l'accueil Samsung ait été affiché puis quitté.

> **Détail d'implémentation important** : la surveillance tourne sur un `System.Threading.Timer`. Une première version utilisait une minuterie NUI avec `await` : quand l'app passait en arrière-plan, la continuation ne reprenait plus et le raccourci ne marchait pas.

## Limites

- Quiet Home doit **tourner**, au premier plan ou en arrière-plan. S'il est fermé, Home redevient celui de Samsung. *Autorun Last App* le relance à l'allumage.
- La barre Samsung est visible une fraction de seconde avant le retour.
- Les touches de raccourci gérées par la TV (Samsung TV Plus, Netflix, Prime…) ne peuvent pas être reprogrammées. Pour vérifier si une touche arrive jusqu'à l'app : Réglages → Récupération → Diagnostic → *Identifier une touche*.
- Testé sur une TV (QN700B, Tizen 6.5). Sur d'autres firmwares, l'identifiant de l'accueil Samsung pourrait différer : les logs l'indiquent (`premier plan → …`).
