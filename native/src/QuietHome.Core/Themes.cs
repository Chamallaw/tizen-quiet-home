using System;
using System.Collections.Generic;
using System.Linq;

namespace QuietHome.Core
{
    /// <summary>Palette d'un thème. Couleurs en #RRGGBB ou #RRGGBBAA.</summary>
    public sealed class ThemeSpec
    {
        public string Id, Name;
        /// <summary>Dégradé de fond, du haut-gauche vers le bas-droit.</summary>
        public string BackgroundFrom, BackgroundTo;
        /// <summary>Puces (réglages) et panneaux.</summary>
        public string Surface;
        public string Text, Muted;
        /// <summary>Puce ou ligne sélectionnée.</summary>
        public string Accent, AccentText;
        public string Overlay;
        /// <summary>Fond des boutons ronds (peut être translucide sur un fond photo).</summary>
        public string Button;
        /// <summary>Trait et liseré de la tuile sélectionnée.</summary>
        public string Marker;
        /// <summary>Image de fond embarquée (res:wallpapers/…), vide = dégradé.</summary>
        public string Wallpaper = "";
        /// <summary>Adoucit les couleurs de marque pour les fondre dans un décor sombre.</summary>
        public bool SoftenTiles;
    }

    /// <summary>Paire de couleurs d'un dégradé de tuile.</summary>
    public sealed class Tone
    {
        public readonly string From, To;
        public Tone(string from, string to) { From = from; To = to; }
    }

    /// <summary>Identité officielle d'une marque : logo embarqué, dégradé de fond, couleur du logo.</summary>
    public sealed class Brand
    {
        public readonly string Key, Mark, MarkColor;
        public readonly Tone Tone;
        public Brand(string key, string mark, string from, string to, string markColor = "#ffffff")
        {
            Key = key; Mark = mark; Tone = new Tone(from, to); MarkColor = markColor;
        }
    }

    /// <summary>
    /// Direction artistique Japandi : neutres chauds, contrastes doux, peu de couleurs, beaucoup d'espace.
    /// </summary>
    public static class Themes
    {
        /// <summary>Valeur de LauncherConfig.Theme : thème clair le jour, sombre le soir.</summary>
        public const string Auto = "auto";
        /// <summary>Thème automatique sans lieu connu : jour de 7 h à 19 h (heure locale).</summary>
        public const int DayStartHour = 7, NightStartHour = 19;

        /// <summary>Ordre du bouton de thème, après « Automatique ». Le premier sert de repli.</summary>
        public static readonly IReadOnlyList<ThemeSpec> All = new List<ThemeSpec>
        {
            // Japandi sombre : mur de tasseaux en noyer, lin et ocre.
            new ThemeSpec { Id = "kuri", Name = "Kuri · noyer", BackgroundFrom = "#2a1d14", BackgroundTo = "#120c08",
                Surface = "#2a2019", Text = "#ece3d4", Muted = "#a8988a", Accent = "#c79a5b", AccentText = "#1a130e",
                Overlay = "#000000aa", Button = "#ece3d41a", Marker = "#c79a5b", Wallpaper = "res:wallpapers/kuri.jpg", SoftenTiles = true },
            // Variante plus grise : chêne fumé, accent sable.
            new ThemeSpec { Id = "fume", Name = "Chêne fumé", BackgroundFrom = "#24211d", BackgroundTo = "#100f0d",
                Surface = "#26231f", Text = "#e6e1da", Muted = "#9d968d", Accent = "#b8a58a", AccentText = "#161412",
                Overlay = "#000000aa", Button = "#e6e1da1a", Marker = "#b8a58a", Wallpaper = "res:wallpapers/fume.jpg", SoftenTiles = true },
            new ThemeSpec { Id = "japandi", Name = "Japandi", BackgroundFrom = "#efeae2", BackgroundTo = "#ddd4c8",
                Surface = "#f7f4ef", Text = "#2b2723", Muted = "#8c8379", Accent = "#2b2723", AccentText = "#f7f4ef", Overlay = "#2b272366",
                Button = "#f7f4ef", Marker = "#2b2723" },
            new ThemeSpec { Id = "sumi", Name = "Sumi", BackgroundFrom = "#24211e", BackgroundTo = "#141311",
                Surface = "#2f2b27", Text = "#ece6dc", Muted = "#8f877c", Accent = "#ece6dc", AccentText = "#1d1b19", Overlay = "#000000aa",
                Button = "#2f2b27", Marker = "#ece6dc" },
            new ThemeSpec { Id = "hinoki", Name = "Hinoki", BackgroundFrom = "#f3e9dc", BackgroundTo = "#dcc7ae",
                Surface = "#faf4ec", Text = "#3a2f26", Muted = "#9a8774", Accent = "#6f7d5c", AccentText = "#faf4ec", Overlay = "#3a2f2666",
                Button = "#faf4ec", Marker = "#3a2f26" },
        };

        /// <summary>Texte posé sur les tuiles en dégradé (toujours sombres) : blanc cassé.</summary>
        public const string CardText = "#f7f3ec";

        /// <summary>
        /// Couleurs officielles (mêmes valeurs que NelsonEU/arn0-tv) ; logos de res/icons, issus de fetch_marks.py.
        /// La clé est cherchée dans le libellé, en minuscules.
        /// </summary>
        public static readonly IReadOnlyList<Brand> Brands = new List<Brand>
        {
            new Brand("netflix", "netflix", "#221f1f", "#000000", "#e50914"),
            new Brand("jellyfin", "jellyfin", "#aa5cc3", "#00a4dc"),
            // Logo blanc sur rouge YouTube : le triangle découpé laisse voir le rouge (variante officielle « blanche »).
            new Brand("youtube", "youtube", "#ff0033", "#9e0020"),
            new Brand("tizentube", "youtube", "#ff0033", "#9e0020"),
            new Brand("spotify", "spotify", "#191414", "#000000", "#1ed760"),
            new Brand("prime", "primevideo", "#0b4f7a", "#1aa0e0"),
            new Brand("disney", "disneyplus", "#091550", "#1f5bd8"),
            new Brand("ps5", "playstation", "#00205c", "#0070d1"),
            new Brand("playstation", "playstation", "#00205c", "#0070d1"),
            new Brand("switch", "nintendoswitch", "#b3000e", "#ff2a2a"),
            new Brand("nintendo", "nintendoswitch", "#b3000e", "#ff2a2a"),
            new Brand("plex", "plex", "#2b2b2b", "#0e0e0e", "#ebaf00"),
            new Brand("twitch", "twitch", "#9146ff", "#4b1fa3"),
            new Brand("apple tv", "appletv", "#2c2c2e", "#000000"),
            new Brand("xbox", "xbox", "#107c10", "#0a3d0a"),
            new Brand("geforce", "nvidia", "#1f1f1f", "#000000", "#76b900"),
            new Brand("shield", "nvidia", "#1f1f1f", "#000000", "#76b900"),
            // Pas de logo libre disponible : le nom s'affiche sur le rouge Free.
            new Brand("free tv", "freetv", "#cd1e25", "#5e0b10"),
        };

        static readonly Tone AllApps = new Tone("#8a8178", "#4b453f");

        static readonly Tone[] Fallback =
        {
            new Tone("#8a7a66", "#54493c"),   // chêne
            new Tone("#6f7d5c", "#414b35"),   // matcha
            new Tone("#9a8574", "#5d4f44"),   // argile
            new Tone("#6b7275", "#3c4144"),   // pierre
            new Tone("#8c6d5b", "#4f3b31"),   // noyer
            new Tone("#76806f", "#454c40"),   // mousse
        };

        /// <summary>Salutation affichée après la date.</summary>
        public static string Greeting(int hour) =>
            hour >= 5 && hour < 18 ? "Bonjour" : hour >= 18 && hour < 23 ? "Bonsoir" : "Bonne nuit";

        public static ThemeSpec Get(string id) => All.FirstOrDefault(t => t.Id == id) ?? All[0];

        public static bool Exists(string id) => All.Any(t => t.Id == id);

        /// <summary>Bouton Thème : Automatique, puis chaque thème dans l'ordre, puis de nouveau Automatique.</summary>
        public static string Next(string id)
        {
            var ids = new List<string> { Auto };
            ids.AddRange(All.Select(t => t.Id));
            return ids[(ids.IndexOf(id) + 1) % ids.Count];
        }

        /// <summary>Thème précédent dans le même ordre (◀ dans Réglages).</summary>
        public static string Previous(string id)
        {
            var ids = new List<string> { Auto };
            ids.AddRange(All.Select(t => t.Id));
            int i = Math.Max(0, ids.IndexOf(id));
            return ids[(i - 1 + ids.Count) % ids.Count];
        }

        /// <summary>Thème à afficher maintenant : celui choisi, ou en mode automatique celui du moment.</summary>
        public static ThemeSpec Resolve(LauncherConfig cfg, DateTimeOffset now) =>
            cfg.Theme != Auto ? Get(cfg.Theme) : Get(IsDay(cfg, now) ? cfg.DayTheme : cfg.NightTheme);

        /// <summary>Jour : soleil levé au lieu configuré ; sans lieu, entre DayStartHour et NightStartHour.</summary>
        public static bool IsDay(LauncherConfig cfg, DateTimeOffset now) => cfg.HasPlace
            ? Sun.Elevation(now.UtcDateTime, cfg.Latitude.Value, cfg.Longitude.Value) > Sun.Horizon
            : now.Hour >= DayStartHour && now.Hour < NightStartHour;

        /// <summary>Thème clair (fond clair, texte sombre), destiné au jour.</summary>
        public static bool IsLight(ThemeSpec theme)
        {
            var c = ParseColor(theme.BackgroundFrom);
            return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2] > 0.5;
        }

        /// <summary>Nom annoncé par le bouton Thème.</summary>
        public static string Describe(LauncherConfig cfg) => cfg.Theme != Auto ? Get(cfg.Theme).Name
            : "Automatique (jour : " + Get(cfg.DayTheme).Name + ", soir : " + Get(cfg.NightTheme).Name + ")";

        public static Brand BrandFor(string label)
        {
            var l = (label ?? "").ToLowerInvariant();
            return Brands.FirstOrDefault(b => l.Contains(b.Key));
        }

        /// <summary>Ton d'une tuile : officiel si la marque est connue, sinon stable par hachage.</summary>
        public static Tone ToneFor(string label)
        {
            var brand = BrandFor(label);
            if (brand != null) return brand.Tone;
            var l = (label ?? "").ToLowerInvariant();
            if (l == "toutes les apps") return AllApps;
            uint h = 0;
            foreach (var ch in l) h = h * 31 + ch;
            return Fallback[h % (uint)Fallback.Length];
        }

        /// <summary>Désature (sat) et assombrit (lum) une couleur #RRGGBB, en espace TSL.</summary>
        public static string Soften(string hex, double sat = 0.80, double lum = 0.82)
        {
            var c = ParseColor(hex);
            double r = c[0], g = c[1], b = c[2];
            double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            double l = (max + min) / 2, h = 0, s = 0, d = max - min;
            if (d > 1e-9)
            {
                s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
                h = max == r ? (g - b) / d + (g < b ? 6 : 0) : max == g ? (b - r) / d + 2 : (r - g) / d + 4;
                h /= 6;
            }
            l *= lum;
            s *= sat;
            double q = l < 0.5 ? l * (1 + s) : l + s - l * s, p = 2 * l - q;
            double Hue(double t)
            {
                if (t < 0) t += 1;
                if (t > 1) t -= 1;
                if (t < 1.0 / 6) return p + (q - p) * 6 * t;
                if (t < 0.5) return q;
                if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
                return p;
            }
            int B(double v) => (int)Math.Round(Math.Max(0, Math.Min(1, v)) * 255);
            return s < 1e-9
                ? string.Format("#{0:x2}{0:x2}{0:x2}", B(l))
                : string.Format("#{0:x2}{1:x2}{2:x2}", B(Hue(h + 1.0 / 3)), B(Hue(h)), B(Hue(h - 1.0 / 3)));
        }

        /// <summary>Convertit #RRGGBB[AA] en composantes 0..1. Valeur invalide : magenta (visible en test).</summary>
        public static float[] ParseColor(string hex)
        {
            try
            {
                var h = hex.TrimStart('#');
                if (h.Length != 6 && h.Length != 8) throw new FormatException();
                float C(int i) => Convert.ToInt32(h.Substring(i, 2), 16) / 255f;
                return new[] { C(0), C(2), C(4), h.Length == 8 ? C(6) : 1f };
            }
            catch (Exception)
            {
                return new[] { 1f, 0f, 1f, 1f };
            }
        }
    }
}
