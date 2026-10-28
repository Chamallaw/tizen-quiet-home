using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace QuietHome.Core
{
    /// <summary>Prévision d'une heure : instant (UTC), température (°C), code météo WMO, jour ou nuit.</summary>
    public sealed class HourForecast
    {
        public DateTime Utc;
        public double Temperature;
        public int Code;
        public bool IsDay;
    }

    /// <summary>Réponse Open-Meteo, réduite aux séries demandées (timeformat=unixtime).</summary>
    [DataContract]
    public sealed class OpenMeteoResponse
    {
        [DataMember(Name = "hourly")] public OpenMeteoHourly Hourly;
    }

    [DataContract]
    public sealed class OpenMeteoHourly
    {
        [DataMember(Name = "time")] public long[] Time;
        [DataMember(Name = "temperature_2m")] public double?[] Temperature;
        [DataMember(Name = "weather_code")] public int?[] Code;
        [DataMember(Name = "is_day")] public int?[] IsDay;
    }

    /// <summary>Dernière réponse reçue, gardée sur la TV pour afficher la météo dès le démarrage.</summary>
    [DataContract]
    public sealed class WeatherCache
    {
        [DataMember(Order = 1)] public double Latitude;
        [DataMember(Order = 2)] public double Longitude;
        [DataMember(Order = 3)] public long FetchedUnix;
        [DataMember(Order = 4)] public string Response;

        /// <summary>Le cache vaut pour ce lieu (sinon, le lieu a changé : on l'ignore).</summary>
        public bool IsFor(double latitude, double longitude) =>
            Math.Abs(latitude - Latitude) < 0.001 && Math.Abs(longitude - Longitude) < 0.001;
    }

    /// <summary>
    /// Bandeau météo des prochaines 24 h : requête Open-Meteo (sans compte ni clé, données CC BY 4.0),
    /// lecture de la réponse, pictogrammes et libellés. Logique pure ; réseau et cache côté Launcher.
    /// </summary>
    public static class Weather
    {
        /// <summary>Heures affichées.</summary>
        public const int ShownHours = 24;
        /// <summary>Heures demandées : le surplus couvre une journée sans réseau.</summary>
        public const int FetchHours = 48;

        public static string ForecastUrl(double latitude, double longitude) => string.Format(CultureInfo.InvariantCulture,
            "https://api.open-meteo.com/v1/forecast?latitude={0:0.####}&longitude={1:0.####}" +
            "&hourly=temperature_2m,weather_code,is_day&forecast_hours={2}&timeformat=unixtime",
            latitude, longitude, FetchHours);

        /// <summary>Heures exploitables de la réponse ; liste vide si elle est illisible.</summary>
        public static List<HourForecast> Parse(string json)
        {
            var list = new List<HourForecast>();
            try
            {
                var h = ConfigStore.Deserialize<OpenMeteoResponse>(json)?.Hourly;
                if (h?.Time == null || h.Temperature == null || h.Code == null) return list;
                int n = Math.Min(h.Time.Length, Math.Min(h.Temperature.Length, h.Code.Length));
                for (int i = 0; i < n; i++)
                {
                    if (h.Temperature[i] == null || h.Code[i] == null) continue;
                    list.Add(new HourForecast
                    {
                        Utc = DateTimeOffset.FromUnixTimeSeconds(h.Time[i]).UtcDateTime,
                        Temperature = h.Temperature[i].Value,
                        Code = h.Code[i].Value,
                        IsDay = h.IsDay == null || i >= h.IsDay.Length || h.IsDay[i] != 0,
                    });
                }
            }
            catch (Exception)
            {
                list.Clear();
            }
            return list;
        }

        /// <summary>Les heures à afficher : l'heure en cours puis les suivantes, dans l'ordre.</summary>
        public static List<HourForecast> Upcoming(IEnumerable<HourForecast> all, DateTime utcNow, int count = ShownHours)
        {
            var start = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, 0, 0, DateTimeKind.Utc);
            return (all ?? Enumerable.Empty<HourForecast>()).Where(h => h.Utc >= start).OrderBy(h => h.Utc).Take(count).ToList();
        }

        /// <summary>Pictogramme embarqué (res/icons/&lt;nom&gt;.png) d'un code météo WMO.</summary>
        public static string Icon(int code, bool isDay)
        {
            switch (code)
            {
                case 0: return isDay ? "weather-sun" : "weather-moon";
                case 1: case 2: return isDay ? "weather-sun-cloud" : "weather-moon-cloud";
                case 3: return "weather-cloud";
                case 45: case 48: return "weather-fog";
                case 51: case 53: case 55: case 56: case 57: return "weather-drizzle";
                case 71: case 73: case 75: case 77: case 85: case 86: return "weather-snow";
                case 95: case 96: case 99: return "weather-storm";
                default: return code >= 61 && code <= 82 ? "weather-rain" : "weather-cloud";
            }
        }

        /// <summary>Température arrondie, sans « -0 ».</summary>
        public static string Degrees(double celsius) =>
            ((int)Math.Round(celsius, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture) + "°";

        /// <summary>Libellé d'une colonne : « Maint. », puis l'heure (« 16h »), et le jour abrégé à minuit (« jeu. »).</summary>
        public static string HourLabel(DateTime local, bool first, CultureInfo culture) =>
            first ? "Maint." : local.Hour == 0 ? local.ToString("ddd", culture) : local.Hour.ToString(CultureInfo.InvariantCulture) + "h";
    }
}
