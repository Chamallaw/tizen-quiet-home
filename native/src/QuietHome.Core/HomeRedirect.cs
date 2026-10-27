using System;

namespace QuietHome.Core
{
    /// <summary>
    /// Bouton Home → Quiet Home, sans toucher au système : on observe l'app au premier plan
    /// (ApplicationManager) et, quand l'accueil Samsung y apparaît, on ramène Quiet Home.
    ///
    /// Règles :
    /// - Home une fois : retour à Quiet Home ;
    /// - Home une seconde fois peu après (Quiet Home déjà revenu) : l'accueil Samsung reste,
    ///   jusqu'à ce qu'une autre app passe au premier plan ;
    /// - les changements de source (lecteur TV) et les autres apps ne déclenchent rien ;
    /// - au rallumage de la TV (Quiet Home reste en mémoire pendant la veille), si la TV reprend
    ///   la dernière source, l'accueil Samsung ou l'app d'avant la veille, on ramène Quiet Home.
    ///   Une app différente (allumage par une touche Netflix, Prime…) est respectée.
    /// Logique pure, testable sur PC ; l'horloge est fournie par l'appelant.
    /// </summary>
    public sealed class HomeRedirect
    {
        public const string SamsungHome = "com.samsung.tv.csfs";
        public const string TvViewer = "org.tizen.tv-viewer";
        /// <summary>Écart entre deux relevés au-delà duquel on considère que la TV a dormi (processus figé).</summary>
        public static readonly TimeSpan SleepGap = TimeSpan.FromSeconds(60);
        /// <summary>Relevés vides consécutifs (≈ secondes) qui signalent un écran éteint.</summary>
        public const int EmptyPollsForSleep = 2;
        public static readonly TimeSpan EscapeWindow = TimeSpan.FromSeconds(6);
        public static readonly TimeSpan RetryAfter = TimeSpan.FromSeconds(3);

        readonly string self;
        DateTime lastRedirect = DateTime.MinValue;
        bool sawSelfSinceRedirect = true;
        // Accueil Samsung autorisé (double appui ou Récupération) jusqu'à ce qu'on l'ait vu puis quitté.
        bool allowSamsung, samsungSeenWhileAllowed;
        // Veille : détectée par la touche Power, un écran vide ou un trou entre deux relevés.
        DateTime lastObserve = DateTime.MinValue;
        int emptyPolls;
        bool slept;
        string beforeSleep, lastForeground;

        public bool Enabled = true;

        public HomeRedirect(string selfAppId) { self = selfAppId; }

        /// <summary>
        /// Choisit l'app « au premier plan » parmi celles que le système déclare comme telles
        /// (il peut y en avoir plusieurs pendant une transition) : Quiet Home d'abord, puis l'accueil Samsung.
        /// </summary>
        public string Pick(System.Collections.Generic.IList<string> foreground)
        {
            if (foreground == null || foreground.Count == 0) return null;
            if (foreground.Contains(self)) return self;
            if (foreground.Contains(SamsungHome)) return SamsungHome;
            return foreground[0];
        }

        /// <summary>Touche Power reçue : la TV va s'éteindre.</summary>
        public void NotePowerOff()
        {
            if (!slept) beforeSleep = lastForeground;
            slept = true;
        }

        /// <summary>L'utilisateur a demandé l'accueil Samsung (Récupération) : ne pas l'intercepter.</summary>
        public void AllowSamsungHome()
        {
            allowSamsung = true;
            samsungSeenWhileAllowed = false;
        }

        /// <summary>
        /// À appeler à chaque relevé de l'app au premier plan.
        /// Retourne true s'il faut ramener Quiet Home maintenant.
        /// </summary>
        public bool Observe(string foreground, DateTime now)
        {
            if (lastObserve != DateTime.MinValue && now - lastObserve > SleepGap) NotePowerOff();
            lastObserve = now;
            if (foreground == null)
            {
                // Un relevé vide isolé arrive pendant les transitions ; plusieurs de suite = écran éteint.
                if (++emptyPolls >= EmptyPollsForSleep) NotePowerOff();
                return false;
            }
            emptyPolls = 0;
            if (slept)
            {
                slept = false;
                lastForeground = foreground;
                if (foreground == self) return false;
                bool resumed = foreground == TvViewer || foreground == SamsungHome || foreground == beforeSleep;
                if (!Enabled || !resumed) return false;
                lastRedirect = now;
                sawSelfSinceRedirect = false;
                allowSamsung = samsungSeenWhileAllowed = false;
                return true;
            }
            lastForeground = foreground;
            if (foreground == self)
            {
                sawSelfSinceRedirect = true;
                // Retour à Quiet Home après avoir réellement utilisé l'accueil Samsung : l'autorisation tombe.
                if (allowSamsung && samsungSeenWhileAllowed) allowSamsung = samsungSeenWhileAllowed = false;
                return false;
            }
            if (foreground != SamsungHome)
            {
                // Une autre app ou une source HDMI : jamais interceptée, et l'autorisation est consommée.
                allowSamsung = samsungSeenWhileAllowed = false;
                return false;
            }
            if (!Enabled) return false;
            if (allowSamsung)
            {
                samsungSeenWhileAllowed = true;
                return false;
            }
            if (!sawSelfSinceRedirect)
            {
                // Retour demandé mais Quiet Home n'est pas encore apparu : on patiente, puis on réessaie.
                if (now - lastRedirect < RetryAfter) return false;
                lastRedirect = now;
                return true;
            }
            if (now - lastRedirect < EscapeWindow)
            {
                // Deuxième appui sur Home juste après le retour : l'utilisateur veut l'accueil Samsung.
                allowSamsung = samsungSeenWhileAllowed = true;
                return false;
            }
            lastRedirect = now;
            sawSelfSinceRedirect = false;
            return true;
        }
    }
}
