using System.Collections.Generic;

namespace QuietHome.Core
{
    /// <summary>
    /// Configuration d'usine. Les IDs d'apps viennent d'une Samsung QE65QN700B (2022) ; un favori absent
    /// de votre TV s'affiche grisé et se retire depuis l'accueil (OK maintenu).
    /// </summary>
    public static class DefaultConfig
    {
        public const string FreeTvName = "Free TV";
        public const string YouTubeName = "YouTube";

        public static LauncherConfig Create()
        {
            return new LauncherConfig
            {
                Pinned = new List<PinSpec>
                {
                    new PinSpec("Netflix", "netflix", "org.tizen.netflix-app"),
                    new PinSpec(YouTubeName, "tizentube", "xvvl3S1TT1.TizenTubeStandalone", "com.samsung.tv.cobalt-yt"),
                    new PinSpec("Jellyfin", "jellyfin", "AprZAARz4r.Jellyfin"),
                    new PinSpec("Spotify", "spotify", "rJeHak5zRg.Spotify"),
                    new PinSpec("Prime Video", "prime video", "org.tizen.primevideo"),
                    new PinSpec("Disney+", "disney", "MCmYXNxgcu.DisneyPlus"),
                    new PinSpec(FreeTvName, "free tv", "znCs95n93Q.OqeeTV"),
                },
                // Épuré : la bibliothèque complète s'ouvre depuis « Toutes les apps ».
                ShowLibrary = false,
                Clock24h = true,
                Theme = "kuri",
                HiddenPrefixes = new List<string>
                {
                    // Services système et notre propre paquet.
                    "com.samsung.", "org.tizen.", "QHLauncher.",
                },
                AlwaysVisible = new List<string>
                {
                    "org.tizen.netflix-app", "org.tizen.primevideo", "org.tizen.browser", "com.samsung.tv.aria-video",
                },
                Hidden = new List<string>
                {
                    // Entrées promotionnelles ou utilitaires relevées sur cette TV.
                    "P6mKfXI3eC.FRPublicValueApp", "J9fZJDMi1v.SecurityCenter", "niftytizen.NiftyGateway",
                },
                // connect_source : 13 = HDMI 1 … 16 = HDMI 4 (relevé sur U8000F, à confirmer sur la QN700B).
                Inputs = new List<InputSpec>
                {
                    new InputSpec("PS5", 14),
                    new InputSpec("Switch", 16),
                },
                // Vide : les logs UDP partent vers le PC déclaré dans le mode développeur de la TV (s'il est actif).
                DevLogHost = "",
            };
        }
    }
}
