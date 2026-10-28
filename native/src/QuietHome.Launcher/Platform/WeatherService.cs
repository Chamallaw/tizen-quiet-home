using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using QuietHome.Core;

namespace QuietHome.Platform
{
    /// <summary>
    /// Prévisions Open-Meteo : une requête HTTPS par heure au plus, réponse gardée dans data/weather.json
    /// pour afficher le bandeau dès le démarrage. Appelé hors du fil NUI (Task.Run) ; ne lève jamais.
    /// </summary>
    public sealed class WeatherService
    {
        static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        readonly string cachePath;

        public WeatherService(string cachePath) { this.cachePath = cachePath; }

        /// <summary>Instant (UTC) de la dernière réponse reçue, MinValue si aucune.</summary>
        public DateTime FetchedUtc { get; private set; } = DateTime.MinValue;

        /// <summary>Prévisions du cache pour ce lieu, ou null (absent, illisible, autre lieu).</summary>
        public List<HourForecast> LoadCache(double latitude, double longitude)
        {
            try
            {
                if (!File.Exists(cachePath)) return null;
                var cache = ConfigStore.Deserialize<WeatherCache>(File.ReadAllText(cachePath, Encoding.UTF8));
                if (cache == null || !cache.IsFor(latitude, longitude)) return null;
                FetchedUtc = DateTimeOffset.FromUnixTimeSeconds(cache.FetchedUnix).UtcDateTime;
                return Weather.Parse(cache.Response);
            }
            catch (Exception e)
            {
                Log.Warn("cache météo illisible : " + e.Message);
                return null;
            }
        }

        /// <summary>Télécharge les prévisions et les met en cache ; null en cas d'échec (journalisé).</summary>
        public List<HourForecast> Fetch(double latitude, double longitude)
        {
            string json;
            List<HourForecast> hours;
            try
            {
                json = http.GetStringAsync(Weather.ForecastUrl(latitude, longitude)).GetAwaiter().GetResult();
                hours = Weather.Parse(json);
            }
            catch (Exception e)
            {
                Log.Warn("météo indisponible : " + e.GetType().Name + " " + e.Message);
                return null;
            }
            if (hours.Count == 0)
            {
                Log.Warn("météo : réponse inexploitable");
                return null;
            }
            FetchedUtc = DateTime.UtcNow;
            Log.Info("météo : " + hours.Count + " heures reçues");
            try
            {
                var cache = new WeatherCache { Latitude = latitude, Longitude = longitude,
                    FetchedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Response = json };
                var tmp = cachePath + ".tmp";
                File.WriteAllText(tmp, ConfigStore.Serialize(cache), Encoding.UTF8);
                if (File.Exists(cachePath)) File.Delete(cachePath);
                File.Move(tmp, cachePath);
            }
            catch (Exception e)
            {
                Log.Warn("cache météo non enregistré : " + e.Message);
            }
            return hours;
        }
    }
}
