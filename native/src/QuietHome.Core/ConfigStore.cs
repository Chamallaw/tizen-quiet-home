using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace QuietHome.Core
{
    /// <summary>
    /// Persistance JSON atomique (écriture .tmp puis remplacement, avec .bak).
    /// Ne lève jamais : un fichier illisible ramène aux défauts, pour que l'accueil s'affiche toujours.
    /// </summary>
    public sealed class ConfigStore
    {
        readonly string path;

        public ConfigStore(string path) { this.path = path; }

        public string Path => path;
        public string LastError { get; private set; }

        /// <summary>Réglages de l'installation (res/local.json), appliqués à chaque chargement ; null = aucun.</summary>
        public LocalOverrides Overrides { get; set; }

        public LauncherConfig Load()
        {
            LastError = null;
            foreach (var candidate in new[] { path, path + ".bak" })
            {
                try
                {
                    if (!File.Exists(candidate)) continue;
                    var cfg = Deserialize<LauncherConfig>(File.ReadAllText(candidate, Encoding.UTF8));
                    if (cfg != null) return WithOverrides(Normalize(cfg));
                }
                catch (Exception e)
                {
                    LastError = candidate + ": " + e.Message;
                }
            }
            return WithOverrides(DefaultConfig.Create());
        }

        /// <summary>Lit res/local.json ; null s'il est absent ou illisible (l'accueil démarre quand même).</summary>
        public static LocalOverrides ReadOverrides(string file)
        {
            try
            {
                return File.Exists(file) ? Deserialize<LocalOverrides>(File.ReadAllText(file, Encoding.UTF8)) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        LauncherConfig WithOverrides(LauncherConfig cfg)
        {
            if (Overrides == null) return cfg;
            cfg.Place = Overrides.Place ?? "";
            cfg.Latitude = Overrides.Latitude;
            cfg.Longitude = Overrides.Longitude;
            CheckPlace(cfg);
            return cfg;
        }

        /// <summary>Un lieu incomplet ou hors bornes est ignoré (pas de météo, thème à heures fixes).</summary>
        static void CheckPlace(LauncherConfig cfg)
        {
            if (cfg.Place == null) cfg.Place = "";
            bool valid = cfg.Latitude is double lat && cfg.Longitude is double lon
                && lat >= -90 && lat <= 90 && lon >= -180 && lon <= 180;
            if (!valid) { cfg.Latitude = null; cfg.Longitude = null; }
        }

        public bool Save(LauncherConfig cfg)
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, Serialize(cfg), Encoding.UTF8);
                if (!File.Exists(path)) File.Move(tmp, path);
                else
                {
                    try { File.Replace(tmp, path, path + ".bak"); }
                    catch (Exception)
                    {
                        // Certains systèmes de fichiers refusent le remplacement atomique : copie + renommage.
                        File.Copy(path, path + ".bak", true);
                        File.Delete(path);
                        File.Move(tmp, path);
                    }
                }
                LastError = null;
                return true;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return false;
            }
        }

        public LauncherConfig Reset()
        {
            try { if (File.Exists(path)) File.Delete(path); } catch (Exception e) { LastError = e.Message; }
            return WithOverrides(DefaultConfig.Create());
        }

        /// <summary>Complète les champs absents (fichier d'une version antérieure ou édité à la main).</summary>
        public static LauncherConfig Normalize(LauncherConfig cfg)
        {
            var d = DefaultConfig.Create();
            if (cfg.Version < 2)
            {
                // Migration v1 : on repart des nouveaux défauts en gardant seulement la destination des logs.
                if (!string.IsNullOrEmpty(cfg.DevLogHost)) d.DevLogHost = cfg.DevLogHost;
                return d;
            }
            if (cfg.Pinned == null) cfg.Pinned = d.Pinned;
            if (cfg.Hidden == null) cfg.Hidden = d.Hidden;
            if (cfg.HiddenPrefixes == null) cfg.HiddenPrefixes = d.HiddenPrefixes;
            if (cfg.Inputs == null) cfg.Inputs = d.Inputs;
            if (cfg.AlwaysVisible == null) cfg.AlwaysVisible = d.AlwaysVisible;
            if (string.IsNullOrEmpty(cfg.Theme)) cfg.Theme = d.Theme;
            if (cfg.Wallpaper == null) cfg.Wallpaper = "";
            if (string.IsNullOrEmpty(cfg.SamsungHomeAppId)) cfg.SamsungHomeAppId = d.SamsungHomeAppId;
            if (string.IsNullOrEmpty(cfg.SettingsAppId)) cfg.SettingsAppId = d.SettingsAppId;
            if (string.IsNullOrEmpty(cfg.TvViewerAppId)) cfg.TvViewerAppId = d.TvViewerAppId;
            if (cfg.DevLogHost == null) cfg.DevLogHost = "";
            if (cfg.DevLogPort <= 0) cfg.DevLogPort = d.DevLogPort;
            if (cfg.AmbientMinutes == 0) cfg.AmbientMinutes = d.AmbientMinutes;
            if (cfg.HomeKey != LauncherConfig.HomeKeySamsung) cfg.HomeKey = LauncherConfig.HomeKeyQuietHome;
            if (cfg.Weather != LauncherConfig.WeatherOff) cfg.Weather = LauncherConfig.WeatherOn;
            // Absent, ancien « on » ou inconnu : le son par défaut.
            if (cfg.Sounds != LauncherConfig.SoundsOff && Array.IndexOf(LauncherConfig.CursorSounds, cfg.Sounds) < 0)
                cfg.Sounds = LauncherConfig.CursorSounds[0];
            if (Array.IndexOf(Lang.Choices, cfg.Language) < 0) cfg.Language = Lang.Auto;
            if (!Themes.Exists(cfg.DayTheme)) cfg.DayTheme = d.DayTheme;
            if (!Themes.Exists(cfg.NightTheme)) cfg.NightTheme = d.NightTheme;
            CheckPlace(cfg);
            foreach (var p in cfg.Pinned) if (p.AppIds == null) p.AppIds = new System.Collections.Generic.List<string>();
            if (cfg.Version < 3)
            {
                // Migration v3 : Free TV rejoint les favoris existants (en dernier), s'il n'y est pas déjà.
                var free = d.Pinned.Find(p => p.Name == DefaultConfig.FreeTvName);
                if (!cfg.Pinned.Exists(p => p.Name == free.Name || p.AppIds.Exists(free.AppIds.Contains)))
                    cfg.Pinned.Add(free);
            }
            if (cfg.Version < 4)
            {
                // Migration v4 : YouTube rejoint les favoris, juste après Netflix (sinon en tête), s'il n'y est pas déjà.
                var yt = d.Pinned.Find(p => p.Name == DefaultConfig.YouTubeName);
                if (!cfg.Pinned.Exists(p => p.Name == yt.Name || p.AppIds.Exists(yt.AppIds.Contains)))
                {
                    int netflix = cfg.Pinned.FindIndex(p => p.Name == "Netflix");
                    cfg.Pinned.Insert(netflix + 1, yt);
                }
            }
            if (cfg.Version < 5 && cfg.Theme != Themes.Auto)
            {
                // Migration v5 : thème automatique. Le thème choisi garde son moment : le jour s'il est clair, le soir sinon.
                var chosen = Themes.Get(cfg.Theme);
                if (Themes.IsLight(chosen)) cfg.DayTheme = chosen.Id; else cfg.NightTheme = chosen.Id;
                cfg.Theme = Themes.Auto;
            }
            cfg.Version = LauncherConfig.CurrentVersion;
            return cfg;
        }

        public static string Serialize<T>(T value)
        {
            var ser = new DataContractJsonSerializer(typeof(T));
            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, value);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static T Deserialize<T>(string json) where T : class
        {
            var ser = new DataContractJsonSerializer(typeof(T));
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return ser.ReadObject(ms) as T;
            }
        }

        public static LauncherConfig Clone(LauncherConfig cfg) => Deserialize<LauncherConfig>(Serialize(cfg));
    }
}
