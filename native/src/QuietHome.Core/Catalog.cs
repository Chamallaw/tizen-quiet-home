using System;
using System.Collections.Generic;
using System.Linq;

namespace QuietHome.Core
{
    /// <summary>Transforme apps installées + configuration en rangées affichables. Fonctions pures.</summary>
    public static class Catalog
    {
        public const string RowFavorites = "favorites";
        public const string RowInputs = "inputs";
        public const string RowLibrary = "library";
        /// <summary>Rangée « barre du haut » : la seule tuile Réglages, dessinée en haut à droite.</summary>
        public const string RowQuick = "quick";

        /// <summary>Préfixe des icônes embarquées dans le paquet (dossier res/ de l'app).</summary>
        public const string ResPrefix = "res:";

        // Actions internes (tuile Réglages et son panneau).
        public const string ActMenu = "menu";
        public const string ActCycleTheme = "cycle-theme";
        public const string ActVolumeDown = "volume-down";
        public const string ActVolumeUp = "volume-up";
        public const string ActMute = "mute";
        public const string ActSettings = "settings";
        public const string ActTheme = "theme";
        public const string ActRecovery = "recovery";
        public const string ActAllApps = "all-apps";

        public static AppEntry ResolvePin(PinSpec pin, IList<AppEntry> apps)
        {
            foreach (var id in pin.AppIds)
            {
                var hit = apps.FirstOrDefault(a => a.Id == id);
                if (hit != null) return hit;
            }
            if (!string.IsNullOrEmpty(pin.Match))
            {
                return apps.FirstOrDefault(a => a.Label != null &&
                    a.Label.IndexOf(pin.Match, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            return null;
        }

        public static bool IsHidden(string id, LauncherConfig cfg)
        {
            if (cfg.Hidden.Contains(id)) return true;
            if (cfg.AlwaysVisible.Contains(id)) return false;
            return cfg.HiddenPrefixes.Any(p => id.StartsWith(p, StringComparison.Ordinal));
        }

        /// <summary>Apps lançables (hors système et masquées), triées : pour « Toutes les apps ».</summary>
        public static List<AppEntry> VisibleApps(IList<AppEntry> apps, LauncherConfig cfg) =>
            (apps ?? new List<AppEntry>())
                .Where(a => !string.IsNullOrEmpty(a.Id) && !a.NoDisplay && !IsHidden(a.Id, cfg))
                .OrderBy(a => a.Label ?? a.Id, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

        /// <summary>
        /// Tuiles de la grille « Toutes les apps » : logo officiel si la marque est connue,
        /// état favori et index d'épinglage repris de la rangée Applications.
        /// </summary>
        public static List<Tile> AllAppTiles(IList<AppEntry> apps, LauncherConfig cfg)
        {
            var pins = new Dictionary<string, int>();
            for (int i = 0; i < cfg.Pinned.Count; i++)
            {
                var hit = ResolvePin(cfg.Pinned[i], apps ?? new List<AppEntry>());
                if (hit != null && !pins.ContainsKey(hit.Id)) pins[hit.Id] = i;
            }
            return VisibleApps(apps, cfg).Select(a => new Tile
            {
                Key = "all:" + a.Id, Kind = TileKind.App, AppId = a.Id, Label = a.Label ?? a.Id,
                IconPath = MarkFor(a.Label) ?? a.IconPath,
                Pinned = pins.ContainsKey(a.Id), PinIndex = pins.TryGetValue(a.Id, out var p) ? p : -1,
            }).ToList();
        }

        /// <summary>Signature de la liste d'apps : évite de tout reconstruire quand rien n'a changé.</summary>
        public static string Signature(IList<AppEntry> apps) =>
            string.Join("|", (apps ?? new List<AppEntry>()).Select(a => a.Id + "=" + a.Label + (a.NoDisplay ? "!" : "")).OrderBy(s => s, StringComparer.Ordinal));

        public static List<Row> BuildRows(IList<AppEntry> apps, LauncherConfig cfg)
        {
            apps = apps ?? new List<AppEntry>();
            var rows = new List<Row>();
            var pinnedIds = new HashSet<string>();

            var fav = new Row { Id = RowFavorites, Title = "Applications" };
            for (int i = 0; i < cfg.Pinned.Count; i++)
            {
                var pin = cfg.Pinned[i];
                var app = ResolvePin(pin, apps);
                if (app != null)
                {
                    if (!pinnedIds.Add(app.Id)) continue;
                    var label = pin.Name ?? app.Label;
                    fav.Tiles.Add(new Tile { Key = "fav:" + app.Id, Kind = TileKind.App, AppId = app.Id,
                        Label = label, IconPath = MarkFor(label) ?? app.IconPath, Pinned = true, PinIndex = i });
                }
                else
                {
                    // Une liste vide signifie « apps pas encore chargées » : on garde la tuile, grisée.
                    fav.Tiles.Add(new Tile { Key = "fav:" + pin.Name, Kind = TileKind.App, AppId = pin.AppIds.FirstOrDefault(),
                        Label = pin.Name, IconPath = MarkFor(pin.Name), Pinned = true, Missing = true, PinIndex = i });
                }
            }
            // Épuré : la bibliothèque complète s'ouvre depuis la dernière tuile de la rangée.
            if (!cfg.ShowLibrary)
            {
                var all = Act(ActAllApps, "Toutes les apps");
                all.IconPath = ResPrefix + "icons/apps.png";
                fav.Tiles.Add(all);
            }

            var quick = new Row { Id = RowQuick, Title = "" };
            var theme = Act(ActCycleTheme, "Thème");
            theme.IconPath = ResPrefix + "icons/theme.png";
            quick.Tiles.Add(theme);
            var menu = Act(ActMenu, "Réglages");
            menu.IconPath = ResPrefix + "icons/settings.png";
            quick.Tiles.Add(menu);
            // Toujours en tête : « haut » depuis les applications mène aux réglages et à la récupération.
            rows.Add(quick);
            rows.Add(fav);

            // Appareils HDMI réglés dans l'app (Réglages → Entrées HDMI).
            var shownInputs = cfg.Inputs.Where(i => i.Enabled && HdmiPorts.KindOf(i).Tile).ToList();
            var inputs = new Row { Id = RowInputs, Title = HdmiPorts.RowTitle(shownInputs) };
            for (int i = 0; i < cfg.Inputs.Count; i++)
            {
                var input = cfg.Inputs[i];
                if (!shownInputs.Contains(input)) continue;
                var icon = InputIcon(input);
                inputs.Tiles.Add(new Tile { Key = "in:" + i, Kind = TileKind.Input, Label = input.Label,
                    Action = input.SourceId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    IconPath = icon == null ? null : ResPrefix + "icons/" + icon + ".png" });
            }
            if (inputs.Tiles.Count > 0) rows.Add(inputs);

            if (cfg.ShowLibrary)
            {
                var lib = new Row { Id = RowLibrary, Title = "Bibliothèque" };
                lib.Tiles.AddRange(apps
                    .Where(a => !string.IsNullOrEmpty(a.Id) && !a.NoDisplay && !pinnedIds.Contains(a.Id) && !IsHidden(a.Id, cfg))
                    .OrderBy(a => a.Label ?? a.Id, StringComparer.CurrentCultureIgnoreCase)
                    .Select(a => new Tile { Key = "lib:" + a.Id, Kind = TileKind.App, AppId = a.Id, Label = a.Label ?? a.Id,
                        IconPath = MarkFor(a.Label) ?? a.IconPath }));
                rows.Add(lib);
            }


            return rows;
        }

        /// <summary>Logo officiel embarqué d'une marque connue (res:icons/…), sinon null.</summary>
        public static string MarkFor(string label)
        {
            var brand = Themes.BrandFor(label);
            return brand == null ? null : ResPrefix + "icons/" + brand.Mark + ".png";
        }

        /// <summary>Logo embarqué d'une console : explicite, sinon celui de la marque reconnue dans le libellé.</summary>
        public static string InputIcon(InputSpec input)
        {
            // Une config v2 peut encore nommer « ps5 »/« switch » : on retombe sur le logo officiel.
            if (!string.IsNullOrEmpty(input.Icon)) return Themes.BrandFor(input.Icon)?.Mark ?? input.Icon;
            return Themes.BrandFor(input.Label)?.Mark;
        }

        static Tile Act(string action, string label) =>
            new Tile { Key = "act:" + action, Kind = TileKind.Action, Action = action, Label = label };

        static int FindPin(LauncherConfig cfg, Tile tile)
        {
            if (tile.PinIndex >= 0 && tile.PinIndex < cfg.Pinned.Count) return tile.PinIndex;
            return cfg.Pinned.FindIndex(p => p.AppIds.Contains(tile.AppId));
        }

        public static LauncherConfig TogglePin(LauncherConfig cfg, Tile tile)
        {
            var next = ConfigStore.Clone(cfg);
            int idx = FindPin(next, tile);
            if (idx >= 0) next.Pinned.RemoveAt(idx);
            else if (!string.IsNullOrEmpty(tile.AppId)) next.Pinned.Add(new PinSpec(tile.Label, null, tile.AppId));
            return next;
        }

        public static LauncherConfig Hide(LauncherConfig cfg, Tile tile)
        {
            var next = ConfigStore.Clone(cfg);
            if (string.IsNullOrEmpty(tile.AppId)) return next;
            if (!next.Hidden.Contains(tile.AppId)) next.Hidden.Add(tile.AppId);
            int idx = FindPin(next, tile);
            if (idx >= 0) next.Pinned.RemoveAt(idx);
            return next;
        }

        public static LauncherConfig UnhideAll(LauncherConfig cfg)
        {
            var next = ConfigStore.Clone(cfg);
            next.Hidden.Clear();
            return next;
        }

        public static LauncherConfig MovePin(LauncherConfig cfg, Tile tile, int delta)
        {
            var next = ConfigStore.Clone(cfg);
            int idx = FindPin(next, tile);
            int target = idx + delta;
            if (idx < 0 || target < 0 || target >= next.Pinned.Count) return next;
            var tmp = next.Pinned[idx];
            next.Pinned[idx] = next.Pinned[target];
            next.Pinned[target] = tmp;
            return next;
        }
    }
}
