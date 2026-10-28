using System;
using System.Collections.Generic;
using System.Globalization;

namespace QuietHome.Core
{
    /// <summary>
    /// Langue de l'interface : français (textes du code) ou anglais (table ci-dessous).
    /// Le texte français sert de clé : un texte absent de la table reste en français.
    /// </summary>
    public static class Lang
    {
        /// <summary>Valeurs du réglage LauncherConfig.Language, dans l'ordre du réglage ◀ ▶.</summary>
        public const string Auto = "auto", French = "fr", English = "en";
        public static readonly string[] Choices = { Auto, French, English };

        /// <summary>Langue en cours (« fr » ou « en »), posée par Use.</summary>
        public static string Current { get; private set; } = French;

        public static CultureInfo Culture => CultureFor(Current);

        public static CultureInfo CultureFor(string lang) => new CultureInfo(lang == English ? "en-GB" : "fr-FR");

        /// <summary>Langue à afficher : celle choisie, ou en automatique celle de la TV (français si elle est en français, anglais sinon).</summary>
        public static string Resolve(string setting, string systemLocale)
        {
            if (setting == French || setting == English) return setting;
            return (systemLocale ?? "").StartsWith("fr", StringComparison.OrdinalIgnoreCase) ? French : English;
        }

        /// <summary>Applique le réglage ; renvoie true si la langue affichée change.</summary>
        public static bool Use(string setting, string systemLocale)
        {
            var next = Resolve(setting, systemLocale);
            if (next == Current) return false;
            Current = next;
            return true;
        }

        /// <summary>Réglage suivant (ou précédent) : Automatique, Français, English.</summary>
        public static string Next(string setting, int step)
        {
            int i = Math.Max(0, Array.IndexOf(Choices, setting));
            return Choices[((i + step) % Choices.Length + Choices.Length) % Choices.Length];
        }

        /// <summary>Nom d'une langue, écrit dans cette langue.</summary>
        public static string Name(string lang) => lang == English ? "English" : "Français";

        /// <summary>Texte dans la langue en cours.</summary>
        public static string T(string french) => T(french, Current);

        public static string T(string french, string lang) =>
            lang == English && french != null && En.TryGetValue(french, out var en) ? en : french;

        /// <summary>Texte à trous ({0}, {1}…) dans la langue en cours.</summary>
        public static string F(string french, params object[] args) => string.Format(CultureInfo.InvariantCulture, T(french), args);

        static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            // Accueil et rangées
            { "Applications", "Apps" },
            { "Toutes les apps", "All apps" },
            { "Thème", "Theme" },
            { "Réglages", "Settings" },
            { "Bibliothèque", "Library" },
            { "Appareils", "Devices" },
            { "Aucun élément", "Nothing here" },
            { "◀ ▶ pour déplacer · OK pour valider", "◀ ▶ to move · OK to confirm" },
            { "{0} applications", "{0} apps" },
            { "{0} applications · maintenir OK pour les favoris", "{0} apps · hold OK for favorites" },
            { "Maint.", "Now" },
            { "Bonjour", "Good morning" },
            { "Bon après-midi", "Good afternoon" },
            { "Bonsoir", "Good evening" },
            { "Bonne nuit", "Good night" },

            // Thèmes et appareils HDMI
            { "Kuri · noyer", "Kuri · walnut" },
            { "Chêne fumé", "Smoked oak" },
            { "Automatique (jour : {0}, soir : {1})", "Automatic (day: {0}, evening: {1})" },
            { "Rien", "Nothing" },
            { "Décodeur TV", "TV box" },
            { "Barre de son", "Soundbar" },
            { "Appareil", "Device" },

            // Messages
            { "Liste des apps indisponible", "App list unavailable" },
            { "Sauvegarde impossible : {0}", "Could not save: {0}" },
            { "Touche reçue : {0}", "Key received: {0}" },
            { "{0} n'est pas installée", "{0} is not installed" },
            { "Impossible de lancer {0}", "Could not open {0}" },
            { "Paramètres indisponibles", "Settings unavailable" },
            { "Retirée des favoris", "Removed from favorites" },
            { "Ajoutée aux favoris", "Added to favorites" },
            { "{0} masquée", "{0} hidden" },
            { "Seuls les favoris se déplacent", "Only favorites can be moved" },
            { "Appuyez sur une touche…", "Press a key…" },
            { "Aucune touche reçue (gérée par la TV)", "No key received (handled by the TV)" },
            { "{0} (non sauvegardé)", "{0} (not saved)" },
            { "Accueil Samsung introuvable", "Samsung Home not found" },
            { "Apps réaffichées", "Hidden apps restored" },
            { "Configuration d'usine", "Factory settings restored" },
            { "Volume illisible ({0})", "Volume unreadable ({0})" },
            { "Volume refusé ({0})", "Volume refused ({0})" },
            { "Muet illisible ({0})", "Mute unreadable ({0})" },
            { "Muet refusé ({0})", "Mute refused ({0})" },
            { "Son rétabli", "Sound on" },
            { "Muet", "Muted" },
            { "Source {0} refusée ({1})", "Source {0} refused ({1})" },
            { "Non disponible sur cette TV", "Not available on this TV" },
            { "Erreur : {0}", "Error: {0}" },

            // Panneaux
            { "◀ ▶ pour régler · Retour pour fermer", "◀ ▶ to adjust · Back to close" },
            { "OK pour choisir · Retour pour fermer", "OK to select · Back to close" },
            { "Retour pour fermer", "Back to close" },
            { "Ouvrir", "Open" },
            { "Retirer des favoris", "Remove from favorites" },
            { "Ajouter aux favoris", "Add to favorites" },
            { "Déplacer", "Move" },
            { "Masquer", "Hide" },
            { "Favoris", "Favorites" },

            // Réglages
            { "Télévision", "Television" },
            { "Paramètres de la TV", "TV settings" },
            { "Accueil Samsung", "Samsung Home" },
            { "Son du curseur", "Cursor sound" },
            { "Météo", "Weather" },
            { "Bouton Home", "Home button" },
            { "Langue", "Language" },
            { "Entrées HDMI", "HDMI inputs" },
            { "Récupération et diagnostic", "Recovery and diagnostics" },
            { "Automatique", "Automatic" },
            { "Automatique · {0}", "Automatic · {0}" },
            { "Désactivé", "Off" },
            { "Goutte", "Drop" },
            { "Bois", "Wood" },
            { "Masquée", "Hidden" },
            { "Affichée", "Shown" },
            { "Affichée · {0}", "Shown · {0}" },
            { "Aucun", "None" },
            { "1 appareil", "1 device" },
            { "{0} appareils", "{0} devices" },
            { "Ce qui est branché", "What is plugged in" },
            { "Chaque appareil a sa tuile sur l'accueil ; OK dessus bascule la TV sur sa prise.",
              "Each device gets a tile on the home screen; OK on it switches the TV to that input." },

            // Récupération et diagnostic
            { "Ouvrir l'accueil Samsung", "Open Samsung Home" },
            { "Paramètres TV", "TV settings" },
            { "Paramètres réseau", "Network settings" },
            { "Gérer les favoris (liste)", "Manage favorites (list)" },
            { "Réafficher les apps masquées", "Show hidden apps again" },
            { "Rafraîchir la liste des apps", "Refresh the app list" },
            { "Réinitialiser la configuration", "Reset all settings" },
            { "Diagnostic", "Diagnostics" },
            { "Démarrage automatique : mode d'emploi", "Open at power-on: how to" },
            { "Fermer Quiet Home", "Close Quiet Home" },
            { "Identifier une touche de la télécommande", "Identify a remote key" },
            { "Lancer l'écran de veille", "Start the screensaver" },
            { "Tester HDMI {0} ({1})", "Test HDMI {0} ({1})" },
            { "Tester Volume +", "Test Volume +" },
            { "Config : {0}", "Config: {0}" },
            { "Fenêtre : {0}", "Window: {0}" },
            { "Langue : {0}", "Language: {0}" },
            { "Thème : {0} → {1}", "Theme: {0} → {1}" },
            { "Météo : aucun lieu (.local/quiethome.json)", "Weather: no place (.local/quiethome.json)" },
            { "Météo : {0} · {1}", "Weather: {0} · {1}" },
            { "pas encore reçue", "not received yet" },
            { "reçue à {0}", "received at {0}" },
            { "— Apps installées (id) —", "— Installed apps (id) —" },
            { "[caché] ", "[hidden] " },
            { "— Journal —", "— Log —" },
            { "Ouvrir Quiet Home à l'allumage", "Open Quiet Home at power-on" },
            { "1. Paramètres › Général › Fonctions Smart", "1. Settings › General › Smart Features" },
            { "2. Exéc. auto dernière app : activé", "2. Autorun Last App: on" },
            { "3. Exécution auto Smart Hub : désactivé", "3. Autorun Smart Hub: off" },
            { "4. Ouvrir Quiet Home puis éteindre la TV", "4. Open Quiet Home, then turn the TV off" },
            { "Ouvrir les paramètres", "Open settings" },
        };

        /// <summary>Clés de la table anglaise, pour les tests (chaque texte à trous garde ses trous).</summary>
        public static IEnumerable<KeyValuePair<string, string>> EnglishTable => En;
    }
}
