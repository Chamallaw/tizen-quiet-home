using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using QuietHome.Core;
using QuietHome.Platform;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

namespace QuietHome.UI
{
    /// <summary>
    /// Écran d'accueil Japandi : grande horloge fine, beaucoup d'air, rangées de cartes en dégradé.
    /// Coordonnées logiques 1920x1080 mises à l'échelle de la fenêtre ; positionnement absolu.
    /// </summary>
    public sealed class HomeScreen
    {
        const float RefW = 1920, RefH = 1080;
        const float MarginX = 80, RowsTop = 330, TitleH = 60, RowGap = 40, TileGap = 20;
        const int LongPressMs = 600;
        const float ButtonGap = 24;
        static readonly CultureInfo Fr = new CultureInfo("fr-FR");

        readonly Window window;
        readonly ConfigStore store;
        readonly AppService apps;
        readonly View root, background, rowsLayer, viewport, topBar;
        readonly ImageView wallpaper;
        readonly TextLabel clock, date, toast, hint;
        readonly List<List<TileView>> tileViews = new List<List<TileView>>();
        readonly List<View> tracks = new List<View>();
        readonly List<float> rowTops = new List<float>(), rowHeights = new List<float>();
        readonly List<TileStyle> rowStyles = new List<TileStyle>();
        readonly FocusModel focus = new FocusModel();
        readonly HomeRedirect redirect;
        readonly string selfAppId;
        // Fil dédié : les minuteries NUI et les continuations async ne sont pas fiables quand l'app est en pause.
        System.Threading.Timer homeWatch;
        readonly object redirectLock = new object();
        int watching;
        string lastForeground;
        // Dernière app vue au premier plan (hors Quiet Home et accueil Samsung) : la sélection y revient au retour.
        volatile string lastApp;

        LauncherConfig config;
        Palette palette;
        List<AppEntry> installed = new List<AppEntry>();
        List<Row> rows = new List<Row>();
        OverlayMenu overlay;
        AppsGrid grid;
        View ambient, ambientBlock, ambientDim;
        TextLabel ambientClock, ambientDate;
        Animation ambientDrift;
        DateTime ambientSince;
        bool ambientDimmed;
        bool moving, enterDown, longPressFired, keyProbe;
        Timer toastTimer, longPressTimer, clockTimer, idleTimer, probeTimer;
        DateTime lastInput = DateTime.Now;
        string appsSignature = "";
        Animation themeFade;

        // Météo : bandeau des prochaines 24 h sous les rangées ; une requête par heure au plus.
        const float WeatherTop = 880, WeatherIconSize = 30;
        static readonly TimeSpan WeatherEvery = TimeSpan.FromMinutes(60), WeatherRetry = TimeSpan.FromMinutes(10);
        readonly WeatherService weather;
        readonly View weatherStrip;
        readonly List<TextLabel> weatherHours = new List<TextLabel>(), weatherTemps = new List<TextLabel>();
        readonly List<ImageView> weatherIcons = new List<ImageView>();
        List<HourForecast> forecast = new List<HourForecast>();
        DateTime weatherTried = DateTime.MinValue;
        bool weatherBusy, rowsLeaveRoom = true;

        public HomeScreen(Window window, ConfigStore store, AppService apps, WeatherService weather, string selfAppId)
        {
            this.window = window;
            this.store = store;
            this.apps = apps;
            this.weather = weather;
            this.selfAppId = selfAppId;
            redirect = new HomeRedirect(selfAppId);
            config = store.Load();
            if (store.LastError != null) Log.Warn("config : " + store.LastError);
            palette = new Palette(Themes.Resolve(config, DateTimeOffset.Now));

            float scale = window.WindowSize.Width / RefW;
            root = new View { Size = new Size(RefW, RefH), Scale = new Vector3(scale, scale, 1f),
                PivotPoint = PivotPoint.TopLeft, ParentOrigin = ParentOrigin.TopLeft, PositionUsesPivotPoint = true };
            window.Add(root);

            background = new View { Size = new Size(RefW, RefH) };
            root.Add(background);
            // Décodé à la taille réelle de la fenêtre : net sur la dalle sans charger une texture 4K inutile.
            wallpaper = new ImageView { Size = new Size(RefW, RefH), FittingMode = FittingModeType.ScaleToFill,
                DesiredWidth = (int)window.WindowSize.Width, DesiredHeight = (int)window.WindowSize.Height };
            root.Add(wallpaper);

            clock = new TextLabel { Position = new Position(MarginX - 6, 70), Size = new Size(900, 160), PointSize = 84,
                FontFamily = Palette.Font("extralight") };
            root.Add(clock);
            date = new TextLabel { Position = new Position(MarginX, 228), Size = new Size(1100, 56), PointSize = 22, EnableMarkup = true,
                FontFamily = Palette.Font("light") };
            root.Add(date);
            // Barre du haut : Thème puis Réglages, alignés sur l'horloge, à droite.
            topBar = new View { Position = new Position(RefW - MarginX - 2 * TileStyle.Button.W - ButtonGap, 96),
                Size = new Size(2 * TileStyle.Button.W + ButtonGap, TileStyle.Button.H) };
            root.Add(topBar);

            viewport = new View { Position = new Position(0, RowsTop), Size = new Size(RefW, RefH - RowsTop - 70),
                ClippingMode = ClippingModeType.ClipChildren };
            root.Add(viewport);
            rowsLayer = new View { Size = new Size(RefW, 4000) };
            viewport.Add(rowsLayer);

            // Vide hors du mode déplacement : pas de raccourcis permanents à l'écran.
            hint = new TextLabel { FontFamily = Palette.Font("regular"), Position = new Position(MarginX, RefH - 70), Size = new Size(900, 44), PointSize = 15 };
            root.Add(hint);
            toast = new TextLabel { FontFamily = Palette.Font("regular"), Position = new Position(RefW - MarginX - 800, RefH - 72), Size = new Size(800, 48), PointSize = 16,
                HorizontalAlignment = HorizontalAlignment.End };
            root.Add(toast);

            // Bandeau météo : 24 colonnes (heure, pictogramme, température) créées une fois, puis mises à jour.
            weatherStrip = new View { Position = new Position(MarginX, WeatherTop), Size = new Size(RefW - 2 * MarginX, 110) };
            float cell = (RefW - 2 * MarginX) / Weather.ShownHours;
            for (int i = 0; i < Weather.ShownHours; i++)
            {
                var hour = new TextLabel { Position = new Position(i * cell, 0), Size = new Size(cell, 30), PointSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center, FontFamily = Palette.Font("regular") };
                var icon = new ImageView { Position = new Position(i * cell + (cell - WeatherIconSize) / 2, 38),
                    Size = new Size(WeatherIconSize, WeatherIconSize), DesiredWidth = 64, DesiredHeight = 64 };
                var temp = new TextLabel { Position = new Position(i * cell, 76), Size = new Size(cell, 34), PointSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center, FontFamily = Palette.Font("light") };
                weatherStrip.Add(hour);
                weatherStrip.Add(icon);
                weatherStrip.Add(temp);
                weatherHours.Add(hour);
                weatherIcons.Add(icon);
                weatherTemps.Add(temp);
            }
            weatherStrip.Hide();
            root.Add(weatherStrip);

            focus.HomeRowId = Catalog.RowFavorites;
            ApplyTheme();
            UpdateClock();
            // Premier tick calé sur le changement de minute, puis une fois par minute.
            var now = DateTime.Now;
            clockTimer = new Timer((uint)Math.Max(1000, 60000 - now.Second * 1000 - now.Millisecond + 50));
            clockTimer.Tick += (s, e) => { OnMinute(); clockTimer.Interval = 60000; return true; };
            clockTimer.Start();
            idleTimer = new Timer(30000);
            idleTimer.Tick += (s, e) => { CheckIdle(); return true; };
            idleTimer.Start();
            // Bouton Home → Quiet Home : relevé de l'app au premier plan chaque seconde, y compris en arrière-plan.
            homeWatch = new System.Threading.Timer(_ => WatchHomeKey(), null, 2000, 1000);

            Rebuild();
            StartWeather();
        }

        // ---------- données ----------

        public async void RefreshApps(bool announce)
        {
            try
            {
                var list = await apps.ListAsync();
                var signature = Catalog.Signature(list);
                installed = list;
                // Retour d'une app : rien n'a été installé ou retiré, inutile de tout redessiner.
                if (signature != appsSignature)
                {
                    appsSignature = signature;
                    Rebuild();
                }
                if (announce) Toast(Catalog.VisibleApps(installed, config).Count + " applications");
            }
            catch (Exception e)
            {
                Log.Error("liste des apps", e);
                Toast("Liste des apps indisponible");
            }
        }

        void UpdateConfig(LauncherConfig next, string message = null)
        {
            config = next;
            Toast(store.Save(config) ? message : "Sauvegarde impossible : " + store.LastError);
            Rebuild();
        }

        // ---------- rendu ----------

        void ApplyTheme()
        {
            palette = new Palette(Themes.Resolve(config, DateTimeOffset.Now));
            window.BackgroundColor = Palette.C(palette.Spec.BackgroundTo);
            background.Background = palette.BackgroundVisual();
            // Fond choisi dans la config, sinon celui du thème.
            var wall = TileView.Resolve(!string.IsNullOrEmpty(config.Wallpaper) ? config.Wallpaper : palette.Spec.Wallpaper);
            wallpaper.ResourceUrl = wall ?? "";
            if (wall == null) wallpaper.Hide(); else wallpaper.Show();
            clock.TextColor = palette.Text;
            toast.TextColor = palette.Text;
            date.TextColor = palette.Muted;
            hint.TextColor = palette.Muted;
        }

        static TileStyle StyleFor(Row row) =>
            row.Id == Catalog.RowQuick ? TileStyle.Button : row.Id == Catalog.RowInputs ? TileStyle.Console : TileStyle.Card;

        static IEnumerable<View> Children(View v)
        {
            for (uint i = 0; i < v.ChildCount; i++) yield return v.GetChildAt(i);
        }

        void Rebuild()
        {
            rows = Catalog.BuildRows(installed, config);
            focus.SetRows(rows);

            foreach (var child in Children(rowsLayer).Concat(Children(topBar)).ToList()) { child.Unparent(); child.Dispose(); }
            tileViews.Clear(); tracks.Clear(); rowTops.Clear(); rowHeights.Clear(); rowStyles.Clear();

            float y = 0;
            foreach (var row in rows)
            {
                var style = StyleFor(row);
                if (row.Id == Catalog.RowQuick)
                {
                    // Hors du flux des rangées : posée dans la barre du haut.
                    var buttons = row.Tiles.Select((t, i) => new TileView(t, style, palette)
                        { Position = new Position(i * (style.W + ButtonGap), 0) }).ToList();
                    foreach (var b in buttons) topBar.Add(b);
                    tracks.Add(topBar); tileViews.Add(buttons); rowTops.Add(-1); rowHeights.Add(0); rowStyles.Add(style);
                    continue;
                }
                bool titled = !string.IsNullOrEmpty(row.Title);
                float trackTop = titled ? TitleH : 0;
                float height = trackTop + style.Footprint;

                var section = new View { Position = new Position(0, y), Size = new Size(RefW, height) };
                if (titled)
                {
                    section.Add(new TextLabel { Text = row.Title.ToUpper(Fr), Position = new Position(MarginX, 0), Size = new Size(900, 44),
                        PointSize = 14, TextColor = palette.Muted, FontFamily = Palette.Font("regular") });
                }
                float stride = style.W + TileGap;
                var track = new View { Position = new Position(MarginX, trackTop), Size = new Size(Math.Max(1, row.Tiles.Count) * stride, style.Footprint) };
                var views = new List<TileView>();
                for (int c = 0; c < row.Tiles.Count; c++)
                {
                    var tv = new TileView(row.Tiles[c], style, palette) { Position = new Position(c * stride, 0) };
                    track.Add(tv);
                    views.Add(tv);
                }
                if (row.Tiles.Count == 0)
                {
                    track.Add(new TextLabel { FontFamily = Palette.Font("regular"), Text = "Aucun élément", Size = new Size(900, 56), PointSize = 16, TextColor = palette.Muted });
                }
                section.Add(track);
                rowsLayer.Add(section);
                tracks.Add(track);
                tileViews.Add(views);
                rowTops.Add(y);
                rowHeights.Add(height);
                rowStyles.Add(style);
                y += height + RowGap;
            }
            // Le bandeau météo ne s'affiche que si les rangées lui laissent la place (bibliothèque masquée).
            rowsLeaveRoom = RowsTop + y - RowGap <= WeatherTop - 40;
            UpdateWeather();
            ApplyFocus(false);
        }

        void ApplyFocus(bool animate)
        {
            var anim = animate ? new Animation(TileView.FocusMs) { DefaultAlphaFunction = TileView.Ease() } : null;
            for (int r = 0; r < tileViews.Count; r++)
            {
                int col = focus.Column(r);
                for (int c = 0; c < tileViews[r].Count; c++)
                    tileViews[r][c].SetFocused(r == focus.RowIndex && c == col, moving, animate);

                var style = rowStyles[r];
                if (rowTops[r] < 0) continue; // barre du haut : position fixe
                float stride = style.W + TileGap;
                int visible = (int)((RefW - 2 * MarginX + TileGap) / stride);
                float x = MarginX - Math.Max(0, col - (visible - 2)) * stride;
                if (tileViews[r].Count <= visible) x = MarginX;
                if (anim != null) anim.AnimateTo(tracks[r], "PositionX", x); else tracks[r].PositionX = x;
            }

            // Défilement vertical seulement si la rangée active déborde de la zone visible.
            float y = rowsLayer.PositionY;
            if (focus.RowIndex < rowTops.Count && rowTops[focus.RowIndex] < 0) y = 0;
            else if (focus.RowIndex < rowTops.Count)
            {
                float top = rowTops[focus.RowIndex], bottom = top + rowHeights[focus.RowIndex];
                float viewH = viewport.SizeHeight;
                if (bottom + y > viewH) y = viewH - bottom;
                if (top + y < 0) y = -top;
            }
            if (anim != null) { anim.AnimateTo(rowsLayer, "PositionY", y); anim.Play(); }
            else rowsLayer.PositionY = y;
            hint.Text = moving ? "◀ ▶ pour déplacer · OK pour valider" : "";
        }

        void UpdateClock()
        {
            var now = DateTime.Now;
            clock.Text = config.Clock24h ? now.ToString("HH:mm", Fr) : now.ToString("h:mm tt", CultureInfo.InvariantCulture);
            // Salutation à la couleur d'accent du thème (balisage NUI).
            date.Text = now.ToString("dddd d MMMM", Fr) + "  ·  <color value='" + palette.Spec.Marker + "'>"
                + Themes.Greeting(now.Hour) + "</color>";
            if (ambient != null) UpdateAmbient(now, true);
        }

        /// <summary>Chaque minute : thème du moment, heure, et à chaque heure pleine le bandeau météo.</summary>
        void OnMinute()
        {
            CheckAutoTheme(true);
            UpdateClock();
            if (DateTime.Now.Minute == 0) UpdateWeather();
            RefreshWeather();
        }

        /// <summary>Thème automatique : bascule au lever ou au coucher du soleil, en fondu si l'accueil est affiché.</summary>
        void CheckAutoTheme(bool fade)
        {
            if (config.Theme != Themes.Auto || themeFade != null) return;
            var target = Themes.Resolve(config, DateTimeOffset.Now);
            if (target.Id == palette.Spec.Id) return;
            Log.Info("thème automatique : " + target.Name);
            if (!fade || !Visible || ambient != null)
            {
                ApplyTheme();
                Rebuild();
                return;
            }
            themeFade = new Animation(600) { DefaultAlphaFunction = TileView.Ease() };
            themeFade.AnimateTo(root, "Opacity", 0f);
            themeFade.Finished += (s, e) =>
            {
                ApplyTheme();
                UpdateClock();
                Rebuild();
                themeFade = new Animation(900) { DefaultAlphaFunction = TileView.Ease() };
                themeFade.AnimateTo(root, "Opacity", 1f);
                themeFade.Finished += (s2, e2) => themeFade = null;
                themeFade.Play();
            };
            themeFade.Play();
        }

        void Toast(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            toast.Text = message;
            toastTimer?.Stop();
            toastTimer = new Timer(2500);
            toastTimer.Tick += (s, e) => { toast.Text = ""; return false; };
            toastTimer.Start();
        }

        // ---------- touches ----------

        public void OnKey(Key key)
        {
            string name = key.KeyPressedName;
            lastInput = DateTime.Now;
            if (keyProbe && key.State == Key.StateType.Down)
            {
                keyProbe = false;
                probeTimer?.Stop();
                Log.Info("touche identifiée : " + name);
                Toast("Touche reçue : " + name);
                return;
            }
            if (ambient != null)
            {
                // N'importe quelle touche réveille l'accueil, sans autre effet.
                if (key.State == Key.StateType.Down) ExitAmbient();
                return;
            }
            if (key.State == Key.StateType.Up)
            {
                if (IsEnter(name) && enterDown)
                {
                    enterDown = false;
                    longPressTimer?.Stop();
                    if (!longPressFired && overlay == null) Activate(CurrentTile);
                }
                return;
            }
            if (key.State != Key.StateType.Down) return;

            if (overlay != null)
            {
                // OK est traité au relâchement côté accueil ; dans le panneau, à l'appui.
                if (!(IsEnter(name) && enterDown)) overlay.HandleKey(name);
                return;
            }

            if (grid != null && !IsEnter(name))
            {
                switch (name)
                {
                    case "Left": grid.Move(Direction.Left); break;
                    case "Right": grid.Move(Direction.Right); break;
                    case "Up": grid.Move(Direction.Up); break;
                    case "Down": grid.Move(Direction.Down); break;
                    case "XF86Back": case "Back": case "Escape": case "BackSpace": CloseGrid(); break;
                    case "XF86Yellow": TogglePin(grid.Current); break;
                }
                return;
            }

            // Touches 1 à 9 : lancent directement le favori correspondant.
            if (name.Length == 1 && name[0] >= '1' && name[0] <= '9')
            {
                var favorites = rows.FirstOrDefault(r => r.Id == Catalog.RowFavorites)?.Tiles;
                int index = name[0] - '1';
                if (favorites != null && index < favorites.Count && favorites[index].Kind == TileKind.App) Activate(favorites[index]);
                return;
            }

            switch (name)
            {
                case "Left": Move(Direction.Left); break;
                case "Right": Move(Direction.Right); break;
                case "Up": Move(Direction.Up); break;
                case "Down": Move(Direction.Down); break;
                case "Return":
                case "KP_Enter":
                    if (moving) { moving = false; ApplyFocus(false); break; }
                    if (enterDown) break; // répétition automatique pendant l'appui long
                    enterDown = true;
                    longPressFired = false;
                    longPressTimer = new Timer(LongPressMs);
                    longPressTimer.Tick += (s, e) => { longPressFired = true; ShowTileMenu(CurrentTile); return false; };
                    longPressTimer.Start();
                    break;
                case "XF86Back":
                case "Back":
                case "Escape":
                case "BackSpace":
                    if (moving) { moving = false; ApplyFocus(false); }
                    else apps.Launch(config.TvViewerAppId);
                    break;
                case "XF86Yellow": TogglePin(focus.Current); break;
                case "XF86Blue": HideTile(focus.Current); break;
                case "XF86Green": StartMove(focus.Current); break;
                case "XF86Red": RefreshApps(true); break;
                case "XF86Home": ResetFocus(); break;
                case "XF86PowerOff":
                    lock (redirectLock) redirect.NotePowerOff();
                    Log.Info("touche Power : mise en veille");
                    break;
                default: Log.Info("touche ignorée : " + name); break;
            }
        }

        static bool IsEnter(string name) => name == "Return" || name == "KP_Enter";

        Tile CurrentTile => grid != null ? grid.Current : focus.Current;

        void Move(Direction dir)
        {
            var tile = focus.Current;
            if (moving && tile != null && tile.Pinned && (dir == Direction.Left || dir == Direction.Right))
            {
                // Les favoris restent groupés : on ne dépasse pas la tuile « Toutes les apps ».
                var row = rows[focus.RowIndex].Tiles;
                int col = focus.Column(focus.RowIndex) + (dir == Direction.Left ? -1 : 1);
                if (col < 0 || col >= row.Count || !row[col].Pinned) return;
                int before = focus.Column(focus.RowIndex);
                var next = Catalog.MovePin(config, tile, dir == Direction.Left ? -1 : 1);
                focus.Move(dir);
                if (focus.Column(focus.RowIndex) != before) UpdateConfig(next);
                return;
            }
            if (focus.Move(dir)) ApplyFocus(true);
        }

        public void ResetFocus()
        {
            overlay?.Close();
            moving = false;
            focus.Reset();
            ApplyFocus(false);
        }

        /// <summary>Retour sur Quiet Home (bouton Home, barre Apps) : la sélection reprend sur l'app qu'on vient de quitter.</summary>
        public void ReturnFocus()
        {
            overlay?.Close();
            moving = false;
            focus.Reset();
            var app = lastApp;
            if (app != null) focus.FocusTile(t => t.Kind == TileKind.App && !t.Missing && t.AppId == app);
            ApplyFocus(false);
        }

        // ---------- actions ----------

        void Activate(Tile tile)
        {
            if (tile == null) return;
            switch (tile.Kind)
            {
                case TileKind.App:
                    if (tile.Missing) { Toast(tile.Label + " n'est pas installée"); return; }
                    if (!apps.Launch(tile.AppId)) { Toast("Impossible de lancer " + tile.Label); return; }
                    CloseGrid();
                    break;
                case TileKind.Input:
                    int id = int.Parse(tile.Action, CultureInfo.InvariantCulture);
                    bool ok = TvControls.TryConnectSource(id, out var msg);
                    Toast(ok ? tile.Label : msg);
                    if (ok) apps.Launch(config.TvViewerAppId);
                    break;
                case TileKind.Action:
                    RunAction(tile.Action);
                    break;
            }
        }

        void RunAction(string action)
        {
            switch (action)
            {
                case Catalog.ActAllApps: ShowAllApps(); break;
                case Catalog.ActMenu: ShowSettings(-1); break;
                case Catalog.ActCycleTheme: CycleTheme(); break;
            }
        }

        void OpenSettings(string menuItem)
        {
            var extras = menuItem == null ? null : new Dictionary<string, string> { { "menuitem", menuItem } };
            if (!apps.Launch(config.SettingsAppId, extras)) Toast("Paramètres indisponibles");
        }

        void TogglePin(Tile tile)
        {
            if (tile == null || tile.Kind != TileKind.App) return;
            UpdateConfig(Catalog.TogglePin(config, tile), tile.Pinned ? "Retirée des favoris" : "Ajoutée aux favoris");
            if (grid != null) ShowAllApps(); // état favori à jour dans la grille
        }

        void HideTile(Tile tile)
        {
            if (tile == null || tile.Kind != TileKind.App || tile.Missing) return;
            UpdateConfig(Catalog.Hide(config, tile), tile.Label + " masquée");
            if (grid != null) ShowAllApps();
        }

        void StartMove(Tile tile)
        {
            if (tile == null || !tile.Pinned) { Toast("Seuls les favoris se déplacent"); return; }
            moving = true;
            ApplyFocus(false);
        }

        // ---------- bouton Home ----------

        void WatchHomeKey()
        {
            // Un seul relevé à la fois, même si l'API répond lentement.
            if (System.Threading.Interlocked.Exchange(ref watching, 1) == 1) return;
            try
            {
                var list = apps.ForegroundAsync().GetAwaiter().GetResult();
                bool launch;
                string foreground;
                lock (redirectLock)
                {
                    redirect.Enabled = config.HomeKey == LauncherConfig.HomeKeyQuietHome;
                    foreground = redirect.Pick(list);
                    launch = redirect.Observe(foreground, DateTime.Now);
                }
                if (foreground != lastForeground)
                {
                    lastForeground = foreground;
                    if (foreground != null && foreground != selfAppId && foreground != HomeRedirect.SamsungHome) lastApp = foreground;
                    Log.Info("premier plan → " + (foreground ?? "(aucune)"));
                }
                if (launch)
                {
                    Log.Info("retour à Quiet Home (bouton Home ou rallumage)");
                    apps.Launch(selfAppId);
                }
            }
            catch (Exception e)
            {
                Log.Error("surveillance du bouton Home", e);
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref watching, 0);
            }
        }

        // ---------- météo ----------

        /// <summary>
        /// Bandeau météo : l'heure en cours et les 23 suivantes. Masqué sans lieu, s'il est désactivé,
        /// si les rangées occupent la place ou s'il manque des heures (aucune réponse depuis un jour).
        /// </summary>
        void UpdateWeather()
        {
            var hours = Weather.Upcoming(forecast, DateTime.UtcNow);
            if (!config.HasPlace || config.Weather != LauncherConfig.WeatherOn || !rowsLeaveRoom || hours.Count < Weather.ShownHours)
            {
                weatherStrip.Hide();
                return;
            }
            for (int i = 0; i < hours.Count; i++)
            {
                var local = hours[i].Utc.ToLocalTime();
                weatherHours[i].Text = Weather.HourLabel(local, i == 0, Fr);
                // Maintenant en clair, minuit (jour abrégé) à la couleur d'accent, le reste en retrait.
                weatherHours[i].TextColor = i == 0 ? palette.Text : local.Hour == 0 ? palette.Marker : palette.Muted;
                var icon = TileView.Resolve(Catalog.ResPrefix + "icons/" + Weather.Icon(hours[i].Code, hours[i].IsDay) + ".png") ?? "";
                if (weatherIcons[i].ResourceUrl != icon) weatherIcons[i].ResourceUrl = icon;
                weatherIcons[i].Color = palette.Text;
                weatherTemps[i].Text = Weather.Degrees(hours[i].Temperature);
                weatherTemps[i].TextColor = palette.Text;
            }
            weatherStrip.Show();
        }

        /// <summary>Au démarrage : les prévisions du cache tout de suite, puis une requête si elles datent.</summary>
        async void StartWeather()
        {
            if (!config.HasPlace) return;
            double lat = config.Latitude.Value, lon = config.Longitude.Value;
            try
            {
                var cached = await Task.Run(() => weather.LoadCache(lat, lon));
                if (cached != null && forecast.Count == 0)
                {
                    forecast = cached;
                    UpdateWeather();
                }
            }
            catch (Exception e)
            {
                Log.Error("météo au démarrage", e);
            }
            RefreshWeather();
        }

        /// <summary>Nouvelle requête si la dernière réponse a plus d'une heure (10 min après un échec), hors du fil NUI.</summary>
        async void RefreshWeather()
        {
            if (!config.HasPlace || config.Weather != LauncherConfig.WeatherOn || weatherBusy) return;
            var now = DateTime.UtcNow;
            if (now - weather.FetchedUtc < WeatherEvery || now - weatherTried < WeatherRetry) return;
            weatherBusy = true;
            weatherTried = now;
            double lat = config.Latitude.Value, lon = config.Longitude.Value;
            try
            {
                var hours = await Task.Run(() => weather.Fetch(lat, lon));
                if (hours != null)
                {
                    forecast = hours;
                    UpdateWeather();
                }
            }
            catch (Exception e)
            {
                Log.Error("météo", e);
            }
            finally
            {
                weatherBusy = false;
            }
        }

        // ---------- mode ambiant ----------

        /// <summary>
        /// Appelé au retour au premier plan : pas de mode ambiant hérité de l'arrière-plan ;
        /// thème, heure et météo remis à jour (les minuteries NUI dorment en arrière-plan).
        /// </summary>
        public void Wake()
        {
            lastInput = DateTime.Now;
            ExitAmbient();
            if (themeFade != null)
            {
                // Fondu interrompu par la mise en arrière-plan : on le termine sans animation.
                themeFade.Stop();
                themeFade = null;
                root.Opacity = 1;
                ApplyTheme();
                Rebuild();
            }
            CheckAutoTheme(false);
            UpdateClock();
            UpdateWeather();
            RefreshWeather();
        }

        /// <summary>Premier plan / arrière-plan, signalé par l'application.</summary>
        public bool Visible { get; set; } = true;

        void CheckIdle()
        {
            if (!Visible || ambient != null || overlay != null || config.AmbientMinutes < 0) return;
            if ((DateTime.Now - lastInput).TotalMinutes >= config.AmbientMinutes) EnterAmbient();
        }

        // Mise en page de l'écran de veille (coordonnées 1920x1080).
        const float AmbientX = 160, AmbientY = 560;
        const int AmbientDimAfterMinutes = 20;

        /// <summary>
        /// Écran de veille Japandi : le mur de bois du thème, voilé, dérive très lentement (zoom et glissement
        /// sur 90 s) ; en bas à gauche, une grande heure fine, un trait ocre, la date et la salutation.
        /// Le bloc se déplace un peu chaque minute et l'ensemble s'assombrit après 20 minutes : rien ne marque la dalle.
        /// </summary>
        void EnterAmbient()
        {
            var ocre = palette.Spec.SoftenTiles ? palette.Marker : Palette.C("#c79a5b");
            var linen = Palette.C("#ece3d4");
            ambient = new View { Size = new Size(RefW, RefH), BackgroundColor = Palette.C("#0d0a08"), Opacity = 0 };

            // Thème clair sans mur (le jour) : le mur du thème du soir, l'écran de veille restant sombre.
            var wall = TileView.Resolve(!string.IsNullOrEmpty(config.Wallpaper) ? config.Wallpaper
                : !string.IsNullOrEmpty(palette.Spec.Wallpaper) ? palette.Spec.Wallpaper : Themes.Get(config.NightTheme).Wallpaper);
            if (wall != null)
            {
                var image = new ImageView
                {
                    ResourceUrl = wall, Size = new Size(RefW, RefH), FittingMode = FittingModeType.ScaleToFill,
                    DesiredWidth = (int)window.WindowSize.Width, DesiredHeight = (int)window.WindowSize.Height,
                    PivotPoint = PivotPoint.Center, ParentOrigin = ParentOrigin.Center, PositionUsesPivotPoint = true,
                };
                ambient.Add(image);
                // Respiration très lente du décor : aller-retour de 90 s, en boucle.
                var scale = new KeyFrames();
                scale.Add(0f, new Vector3(1.02f, 1.02f, 1f));
                scale.Add(0.5f, new Vector3(1.08f, 1.08f, 1f));
                scale.Add(1f, new Vector3(1.02f, 1.02f, 1f));
                var slide = new KeyFrames();
                slide.Add(0f, 0f);
                slide.Add(0.5f, -36f);
                slide.Add(1f, 0f);
                ambientDrift = new Animation(90000) { Looping = true, DefaultAlphaFunction = new AlphaFunction(AlphaFunction.BuiltinFunctions.EaseInOutSine) };
                ambientDrift.AnimateBetween(image, "Scale", scale);
                ambientDrift.AnimateBetween(image, "PositionX", slide);
                ambientDrift.Play();
            }
            // Voile : plus léger en haut à gauche (lumière rasante), profond en bas à droite.
            ambient.Add(new View { Size = new Size(RefW, RefH), Background = Palette.Gradient("#0b080694", "#050403e0", 0) });
            ambientDim = new View { Size = new Size(RefW, RefH), BackgroundColor = Color.Black, Opacity = 0 };

            ambientBlock = new View { Position = new Position(AmbientX, AmbientY), Size = new Size(1400, 380) };
            ambientClock = new TextLabel { Size = new Size(1400, 240), PointSize = 168, FontFamily = Palette.Font("extralight"),
                TextColor = new Color(linen.R, linen.G, linen.B, 0.92f) };
            ambientBlock.Add(ambientClock);
            ambientBlock.Add(new View { Position = new Position(10, 262), Size = new Size(72, 3), CornerRadius = 1.5f, BackgroundColor = ocre });
            ambientDate = new TextLabel { Position = new Position(8, 290), Size = new Size(1400, 60), PointSize = 26, EnableMarkup = true,
                FontFamily = Palette.Font("light"), TextColor = new Color(linen.R, linen.G, linen.B, 0.72f) };
            ambientBlock.Add(ambientDate);
            ambient.Add(ambientBlock);

            ambient.Add(new TextLabel { Text = "QUIET HOME", Position = new Position(RefW - 160 - 400, RefH - 110), Size = new Size(400, 40),
                PointSize = 11, HorizontalAlignment = HorizontalAlignment.End, FontFamily = Palette.Font("regular"),
                TextColor = new Color(linen.R, linen.G, linen.B, 0.28f) });
            ambient.Add(ambientDim);
            root.Add(ambient);

            ambientSince = DateTime.Now;
            ambientDimmed = false;
            UpdateAmbient(ambientSince, false);
            var fade = new Animation(2500) { DefaultAlphaFunction = TileView.Ease() };
            fade.AnimateTo(ambient, "Opacity", 1f);
            fade.Play();
            Log.Info("écran de veille");
        }

        /// <summary>Heure, date, dérive du bloc (chaque minute) et assombrissement tardif.</summary>
        void UpdateAmbient(DateTime now, bool animate)
        {
            var ocre = palette.Spec.SoftenTiles ? palette.Spec.Marker : "#c79a5b";
            ambientClock.Text = clock.Text;
            ambientDate.Text = now.ToString("dddd d MMMM", Fr) + "  ·  <color value='" + ocre + "'>" + Themes.Greeting(now.Hour) + "</color>";

            // Dérive sur une petite ellipse : jamais deux minutes de suite au même endroit.
            double a = now.Minute * 0.9;
            var target = new Position(AmbientX + (float)(Math.Cos(a) * 48), AmbientY + (float)(Math.Sin(a * 1.3) * 28));
            if (!animate) ambientBlock.Position = target;
            else
            {
                var move = new Animation(4000) { DefaultAlphaFunction = new AlphaFunction(AlphaFunction.BuiltinFunctions.EaseInOutSine) };
                move.AnimateTo(ambientBlock, "Position", target);
                move.Play();
            }

            if (!ambientDimmed && (now - ambientSince).TotalMinutes >= AmbientDimAfterMinutes)
            {
                ambientDimmed = true;
                var dim = new Animation(60000);
                dim.AnimateTo(ambientDim, "Opacity", 0.55f);
                dim.Play();
                Log.Info("écran de veille assombri");
            }
        }

        void ExitAmbient()
        {
            if (ambient == null) return;
            ambientDrift?.Stop();
            ambientDrift = null;
            ambient.Unparent();
            ambient.Dispose();
            ambient = null;
        }

        // ---------- diagnostic ----------

        void StartKeyProbe()
        {
            keyProbe = true;
            Toast("Appuyez sur une touche…");
            probeTimer?.Stop();
            probeTimer = new Timer(10000);
            probeTimer.Tick += (s, e) =>
            {
                // Touche gérée par le système (Home, TV Plus…) : elle n'arrive jamais à l'app.
                if (keyProbe) { keyProbe = false; Toast("Aucune touche reçue (gérée par la TV)"); }
                return false;
            };
            probeTimer.Start();
        }

        void TestSource(int id)
        {
            TvControls.TryConnectSource(id, out var msg);
            Toast(msg);
        }

        // ---------- panneaux ----------

        void ShowOverlay(string title, List<OverlayMenu.Item> items, int selection = -1)
        {
            overlay = new OverlayMenu(title, items, palette, new Size(RefW, RefH), selection);
            overlay.Closed += () => overlay = null;
            root.Add(overlay);
        }

        void ShowTileMenu(Tile tile)
        {
            if (tile == null) return;
            if (tile.Kind != TileKind.App) { Activate(tile); return; }
            var items = new List<OverlayMenu.Item>();
            if (!tile.Missing) items.Add(new OverlayMenu.Item("Ouvrir", () => Activate(tile)));
            items.Add(new OverlayMenu.Item(tile.Pinned ? "Retirer des favoris" : "Ajouter aux favoris", () => TogglePin(tile)));
            if (tile.Pinned && !tile.Missing && grid == null) items.Add(new OverlayMenu.Item("Déplacer", () => StartMove(tile)));
            if (!tile.Missing) items.Add(new OverlayMenu.Item("Masquer", () => HideTile(tile)));
            ShowOverlay(tile.Label, items);
        }

        void ShowAllApps()
        {
            CloseGrid();
            grid = new AppsGrid(Catalog.AllAppTiles(installed, config), palette, new Size(RefW, RefH));
            root.Add(grid);
        }

        void CloseGrid()
        {
            grid?.Close();
            grid = null;
        }

        /// <summary>OK bascule l'app dans les favoris ; le panneau se rouvre sur la même ligne.</summary>
        void ShowManageFavorites(int selection)
        {
            var list = Catalog.VisibleApps(installed, config);
            var favorites = Catalog.BuildRows(installed, config).First(r => r.Id == Catalog.RowFavorites).Tiles;
            var items = new List<OverlayMenu.Item>();
            for (int i = 0; i < list.Count; i++)
            {
                var app = list[i];
                int index = i;
                var fav = favorites.FirstOrDefault(t => t.AppId == app.Id && !t.Missing);
                var tile = fav ?? new Tile { Kind = TileKind.App, AppId = app.Id, Label = app.Label };
                items.Add(new OverlayMenu.Item((fav != null ? "●   " : "○   ") + app.Label, () =>
                {
                    UpdateConfig(Catalog.TogglePin(config, tile));
                    ShowManageFavorites(index);
                }));
            }
            ShowOverlay("Favoris", items, selection);
        }

        /// <summary>Bouton Thème : passe au suivant et l'enregistre aussitôt ; le focus reste sur le bouton.</summary>
        void CycleTheme()
        {
            var next = ConfigStore.Clone(config);
            next.Theme = Themes.Next(config.Theme);
            config = next;
            bool saved = store.Save(config);
            if (!saved) Log.Error("thème non sauvegardé : " + store.LastError);
            ApplyTheme();
            UpdateClock();
            Rebuild();
            var name = Themes.Describe(config);
            Toast(saved ? name : name + " (non sauvegardé)");
        }

        /// <summary>Panneau de la tuile Réglages : son, bouton Home, météo, paramètres Samsung, récupération.</summary>
        void ShowSettings(int selection)
        {
            var items = new List<OverlayMenu.Item>
            {
                new OverlayMenu.Item("Volume −", () => Toast(TvControls.ChangeVolume(-2)), keepOpen: true),
                new OverlayMenu.Item("Volume +", () => Toast(TvControls.ChangeVolume(+2)), keepOpen: true),
                new OverlayMenu.Item("Muet", () => Toast(TvControls.ToggleMute()), keepOpen: true),
                new OverlayMenu.Item(config.HomeKey == LauncherConfig.HomeKeyQuietHome
                    ? "Bouton Home : Quiet Home (2 appuis = Samsung)" : "Bouton Home : accueil Samsung", () =>
                {
                    var next = ConfigStore.Clone(config);
                    next.HomeKey = config.HomeKey == LauncherConfig.HomeKeyQuietHome ? LauncherConfig.HomeKeySamsung : LauncherConfig.HomeKeyQuietHome;
                    UpdateConfig(next);
                    ShowSettings(3); // rouvert sur la même ligne, libellé à jour
                }),
            };
            if (config.HasPlace)
            {
                bool on = config.Weather == LauncherConfig.WeatherOn;
                items.Add(new OverlayMenu.Item(on ? "Météo : affichée" + (string.IsNullOrEmpty(config.Place) ? "" : " (" + config.Place + ")")
                    : "Météo : masquée", () =>
                {
                    var next = ConfigStore.Clone(config);
                    next.Weather = on ? LauncherConfig.WeatherOff : LauncherConfig.WeatherOn;
                    UpdateConfig(next);
                    RefreshWeather();
                    ShowSettings(4);
                }));
            }
            items.Add(new OverlayMenu.Item("Paramètres de la TV", () => OpenSettings(null)));
            items.Add(new OverlayMenu.Item("Récupération", ShowRecovery));
            ShowOverlay("Réglages", items, selection);
        }

        void ShowRecovery()
        {
            ShowOverlay("Récupération", new List<OverlayMenu.Item>
            {
                new OverlayMenu.Item("Ouvrir l'accueil Samsung", () =>
                {
                    lock (redirectLock) redirect.AllowSamsungHome();
                    if (!apps.Launch(config.SamsungHomeAppId)) Toast("Accueil Samsung introuvable");
                }),
                new OverlayMenu.Item("Paramètres TV", () => OpenSettings(null)),
                new OverlayMenu.Item("Paramètres réseau", () => OpenSettings("networksettings")),
                new OverlayMenu.Item("Gérer les favoris (liste)", () => ShowManageFavorites(0)),
                new OverlayMenu.Item("Réafficher les apps masquées", () => UpdateConfig(Catalog.UnhideAll(config), "Apps réaffichées")),
                new OverlayMenu.Item("Rafraîchir la liste des apps", () => RefreshApps(true)),
                new OverlayMenu.Item("Réinitialiser la configuration", () => { config = store.Reset(); ApplyTheme(); Rebuild(); Toast("Configuration d'usine"); }),
                new OverlayMenu.Item("Diagnostic", ShowDiagnostic),
                new OverlayMenu.Item("Démarrage automatique : mode d'emploi", ShowAutorunHelp),
                new OverlayMenu.Item("Fermer Quiet Home", () => Tizen.Applications.Application.Current.Exit()),
            });
        }

        void ShowDiagnostic()
        {
            var items = new List<OverlayMenu.Item>
            {
                new OverlayMenu.Item("Identifier une touche de la télécommande", StartKeyProbe),
                new OverlayMenu.Item("Lancer l'écran de veille", EnterAmbient),
                new OverlayMenu.Item("Tester HDMI 1 (13)", () => TestSource(13), keepOpen: true),
                new OverlayMenu.Item("Tester HDMI 2 (14)", () => TestSource(14), keepOpen: true),
                new OverlayMenu.Item("Tester HDMI 3 (15)", () => TestSource(15), keepOpen: true),
                new OverlayMenu.Item("Tester HDMI 4 (16)", () => TestSource(16), keepOpen: true),
                new OverlayMenu.Item("Tester Volume +", () => Toast(TvControls.ChangeVolume(+1)), keepOpen: true),
                new OverlayMenu.Item("Config : " + store.Path),
                new OverlayMenu.Item("Fenêtre : " + window.WindowSize.Width + "x" + window.WindowSize.Height),
                new OverlayMenu.Item("Thème : " + Themes.Describe(config) + " → " + palette.Name),
                new OverlayMenu.Item(!config.HasPlace ? "Météo : aucun lieu (.local/quiethome.json)"
                    : "Météo : " + config.Place + " · " + (weather.FetchedUtc == DateTime.MinValue ? "pas encore reçue"
                        : "reçue à " + weather.FetchedUtc.ToLocalTime().ToString("HH:mm", Fr))),
                new OverlayMenu.Item("— Apps installées (id) —"),
            };
            items.AddRange(installed.OrderBy(a => a.Id).Select(a => new OverlayMenu.Item((a.NoDisplay ? "[caché] " : "") + a.Id + "  " + a.Label)));
            items.Add(new OverlayMenu.Item("— Journal —"));
            items.AddRange(Log.Recent().Reverse().Select(l => new OverlayMenu.Item(l)));
            ShowOverlay("Diagnostic", items);
        }

        void ShowAutorunHelp()
        {
            ShowOverlay("Ouvrir Quiet Home à l'allumage", new List<OverlayMenu.Item>
            {
                new OverlayMenu.Item("1. Paramètres › Général › Fonctions Smart"),
                new OverlayMenu.Item("2. Exéc. auto dernière app : activé"),
                new OverlayMenu.Item("3. Exécution auto Smart Hub : désactivé"),
                new OverlayMenu.Item("4. Ouvrir Quiet Home puis éteindre la TV"),
                new OverlayMenu.Item("Ouvrir les paramètres", () => OpenSettings(null)),
            });
        }
    }
}
