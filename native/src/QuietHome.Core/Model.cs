using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace QuietHome.Core
{
    /// <summary>App installée, telle que remontée par la plateforme.</summary>
    public sealed class AppEntry
    {
        public string Id;
        public string Label;
        public string IconPath;
        public bool NoDisplay;

        public AppEntry(string id, string label, string iconPath = null, bool noDisplay = false)
        {
            Id = id; Label = label; IconPath = iconPath; NoDisplay = noDisplay;
        }
    }

    public enum TileKind { App, Input, Action }

    /// <summary>Élément affichable dans une rangée.</summary>
    public sealed class Tile
    {
        public string Key;
        public TileKind Kind;
        public string Label;
        public string AppId;
        public string IconPath;
        /// <summary>Action interne (Kind == Action) ou index d'entrée HDMI (Kind == Input).</summary>
        public string Action;
        public bool Pinned;
        public bool Missing;
        /// <summary>Index dans LauncherConfig.Pinned, -1 sinon.</summary>
        public int PinIndex = -1;
    }

    public sealed class Row
    {
        public string Id;
        public string Title;
        public List<Tile> Tiles = new List<Tile>();
    }

    [DataContract]
    public sealed class PinSpec
    {
        [DataMember(Order = 1)] public string Name;
        /// <summary>IDs Tizen candidats, le premier installé gagne.</summary>
        [DataMember(Order = 2)] public List<string> AppIds = new List<string>();
        /// <summary>Repli : sous-chaîne du libellé (insensible à la casse).</summary>
        [DataMember(Order = 3)] public string Match;

        public PinSpec() { }
        public PinSpec(string name, string match, params string[] ids)
        {
            Name = name; Match = match; AppIds = new List<string>(ids);
        }
    }

    [DataContract]
    public sealed class InputSpec
    {
        [DataMember(Order = 1)] public string Label;
        /// <summary>Identifiant passé à connect_source (libsource-api). À calibrer par TV.</summary>
        [DataMember(Order = 2)] public int SourceId;
        [DataMember(Order = 3)] public bool Enabled = true;
        /// <summary>Nom d'icône embarquée (res/icons/&lt;Icon&gt;.png) ; vide = déduite du libellé.</summary>
        [DataMember(Order = 4)] public string Icon;
        /// <summary>Type d'appareil (HdmiPorts.Kinds) ; absent = déduit du nom.</summary>
        [DataMember(Order = 5)] public string Kind;

        public InputSpec() { }
        public InputSpec(string label, int sourceId, string icon = null) { Label = label; SourceId = sourceId; Icon = icon; }
    }

    [DataContract]
    public sealed class LauncherConfig
    {
        /// <summary>
        /// v2 : refonte Japandi (favoris, consoles, thèmes) ; une config v1 est remplacée par les défauts.
        /// v3 : ajout de Free TV aux favoris, sans toucher au reste.
        /// v4 : ajout de YouTube (TizenTube sideloadé) juste après Netflix.
        /// v5 : thème automatique ; le thème choisi devient celui de son moment (jour s'il est clair, soir sinon).
        /// </summary>
        public const int CurrentVersion = 5;

        [DataMember(Order = 1)] public int Version = CurrentVersion;
        /// <summary>Identifiant d'un thème, ou « auto » : DayTheme le jour, NightTheme le soir.</summary>
        [DataMember(Order = 2)] public string Theme = Themes.Auto;
        /// <summary>Chemin absolu d'une image de fond ; vide = dégradé du thème.</summary>
        [DataMember(Order = 3)] public string Wallpaper = "";
        [DataMember(Order = 4)] public List<PinSpec> Pinned = new List<PinSpec>();
        [DataMember(Order = 5)] public List<string> Hidden = new List<string>();
        [DataMember(Order = 6)] public List<string> HiddenPrefixes = new List<string>();
        [DataMember(Order = 7)] public List<InputSpec> Inputs = new List<InputSpec>();
        [DataMember(Order = 8)] public bool ShowLibrary = true;
        [DataMember(Order = 9)] public bool Clock24h = true;
        /// <summary>App de secours : Home Samsung d'origine.</summary>
        [DataMember(Order = 10)] public string SamsungHomeAppId = "com.samsung.tv.csfs";
        [DataMember(Order = 11)] public string SettingsAppId = "org.tizen.menu";
        [DataMember(Order = 12)] public string TvViewerAppId = "org.tizen.tv-viewer";
        /// <summary>IP du PC qui reçoit les logs UDP ; vide = PC du mode développeur, sinon aucun.</summary>
        [DataMember(Order = 13)] public string DevLogHost = "";
        [DataMember(Order = 14)] public int DevLogPort = 5140;
        /// <summary>Exceptions aux préfixes masqués (apps utilisateur aux IDs « système »).</summary>
        [DataMember(Order = 15)] public List<string> AlwaysVisible = new List<string>();
        /// <summary>Minutes sans touche avant le mode ambiant ; absent ou 0 = 5 min, négatif = désactivé.</summary>
        [DataMember(Order = 16)] public int AmbientMinutes = 5;
        /// <summary>Bouton Home de la télécommande : « quiethome » (ramène Quiet Home) ou « samsung ».</summary>
        [DataMember(Order = 17)] public string HomeKey = HomeKeyQuietHome;
        [DataMember(Order = 18)] public string DayTheme = "hinoki";
        [DataMember(Order = 19)] public string NightTheme = "kuri";
        /// <summary>
        /// Lieu de la météo et du lever/coucher du soleil (thème automatique). Fourni au build par res/local.json,
        /// tiré de .local/quiethome.json (jamais publié) ; absent = pas de météo, thème de 7 h à 19 h.
        /// </summary>
        [DataMember(Order = 20)] public string Place = "";
        [DataMember(Order = 21)] public double? Latitude;
        [DataMember(Order = 22)] public double? Longitude;
        /// <summary>Bandeau météo des prochaines 24 h : « on » ou « off ».</summary>
        [DataMember(Order = 23)] public string Weather = WeatherOn;
        /// <summary>Son du déplacement du curseur : un nom de CursorSounds (res/sounds/tick-&lt;nom&gt;.wav) ou « off ».</summary>
        [DataMember(Order = 24)] public string Sounds = CursorSounds[0];

        public const string HomeKeyQuietHome = "quiethome", HomeKeySamsung = "samsung";
        public const string WeatherOn = "on", WeatherOff = "off";
        public const string SoundsOff = "off";
        /// <summary>Sons du curseur, dans l'ordre du réglage ; le premier est le son par défaut.</summary>
        public static readonly string[] CursorSounds = { "goutte", "bois", "kalimba" };

        /// <summary>Réglage suivant (ou précédent) du son du curseur : chaque son, puis « off ».</summary>
        public static string NextSound(string current, int step)
        {
            var all = new List<string>(CursorSounds) { SoundsOff };
            int i = Math.Max(0, all.IndexOf(current));
            return all[((i + step) % all.Count + all.Count) % all.Count];
        }

        public bool HasPlace => Latitude.HasValue && Longitude.HasValue;
    }

    /// <summary>
    /// Réglages propres à une installation, embarqués au build dans res/local.json : ils l'emportent
    /// sur la configuration enregistrée à chaque chargement.
    /// </summary>
    [DataContract]
    public sealed class LocalOverrides
    {
        [DataMember(Order = 1)] public string Place;
        [DataMember(Order = 2)] public double? Latitude;
        [DataMember(Order = 3)] public double? Longitude;
    }
}
