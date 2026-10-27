using System;
using QuietHome.Core;
using QuietHome.Platform;
using QuietHome.UI;
using Tizen.Applications;
using Tizen.NUI;

namespace QuietHome
{
    /// <summary>Point d'entrée et cycle de vie Tizen.</summary>
    public sealed class App : NUIApplication
    {
        HomeScreen home;

        protected override void OnCreate()
        {
            base.OnCreate();
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Log.Error("exception non gérée : " + e.ExceptionObject);
            try
            {
                var store = new ConfigStore(System.IO.Path.Combine(DirectoryInfo.Data, "config.json"));
                var cfg = store.Load();
                Log.EnableUdp(string.IsNullOrEmpty(cfg.DevLogHost) ? DeveloperHost() : cfg.DevLogHost, cfg.DevLogPort);
                Log.Info("démarrage " + ApplicationInfo.ApplicationId + " " + typeof(App).Assembly.GetName().Version);

                TileView.ResourceDir = DirectoryInfo.Resource;
                var fonts = System.IO.Path.Combine(DirectoryInfo.Resource, "fonts");
                Log.Info("polices DM Sans : " + (FontClient.Instance.AddCustomFontDirectory(fonts) ? "ok" : "échec"));
                var window = GetDefaultWindow();
                window.Title = "Quiet Home";
                home = new HomeScreen(window, store, new AppService(), ApplicationInfo.ApplicationId);
                window.KeyEvent += (s, e) => home.OnKey(e.Key);
                home.RefreshApps(false);
            }
            catch (Exception e)
            {
                // Dernier filet : on ne laisse pas l'utilisateur devant un écran vide.
                Log.Error("échec au démarrage", e);
                new AppService().Launch("com.samsung.tv.csfs");
                Exit();
            }
        }

        /// <summary>
        /// IP du PC déclaré dans le mode développeur de la TV (API locale, champ developerIP),
        /// ou null si le mode développeur est inactif : pas de logs réseau hors développement.
        /// </summary>
        static string DeveloperHost()
        {
            try
            {
                using (var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) })
                {
                    var json = http.GetStringAsync("http://127.0.0.1:8001/api/v2/").GetAwaiter().GetResult();
                    if (!json.Contains("\"developerMode\":\"1\"")) return null;
                    var m = System.Text.RegularExpressions.Regex.Match(json, "\"developerIP\":\"([0-9.]+)\"");
                    return m.Success && m.Groups[1].Value != "0.0.0.0" ? m.Groups[1].Value : null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        protected override void OnAppControlReceived(AppControlReceivedEventArgs e)
        {
            base.OnAppControlReceived(e);
            // Relance de l'app déjà ouverte (barre Apps ou bouton Home) : retour à l'état d'accueil.
            home?.ResetFocus();
        }

        protected override void OnPause()
        {
            base.OnPause();
            Log.Info("arrière-plan");
        }

        protected override void OnResume()
        {
            base.OnResume();
            Log.Info("premier plan");
            home?.Wake();
            home?.RefreshApps(false);
        }

        static void Main(string[] args)
        {
            using (var app = new App()) app.Run(args);
        }
    }
}
