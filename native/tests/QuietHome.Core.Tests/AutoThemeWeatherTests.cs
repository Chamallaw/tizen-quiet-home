using System;
using System.Globalization;
using System.IO;
using System.Linq;
using QuietHome.Core;
using Xunit;

public class SunTests
{
    const double ParisLat = 48.8566, ParisLon = 2.3522;

    static DateTime Utc(int y, int m, int d, int h, int min) => new DateTime(y, m, d, h, min, 0, DateTimeKind.Utc);
    static bool Up(DateTime utc, double lat = ParisLat, double lon = ParisLon) => Sun.Elevation(utc, lat, lon) > Sun.Horizon;

    [Fact]
    public void Lever_et_coucher_a_Paris_a_quelques_minutes_pres()
    {
        // Solstice d'hiver : lever 8 h 42, coucher 16 h 56 (heure de Paris, UTC+1).
        Assert.False(Up(Utc(2026, 12, 21, 7, 35)));
        Assert.True(Up(Utc(2026, 12, 21, 7, 50)));
        Assert.True(Up(Utc(2026, 12, 21, 15, 50)));
        Assert.False(Up(Utc(2026, 12, 21, 16, 5)));
        // Solstice d'été : lever 5 h 47, coucher 21 h 58 (UTC+2).
        Assert.False(Up(Utc(2026, 6, 21, 3, 40)));
        Assert.True(Up(Utc(2026, 6, 21, 3, 55)));
        Assert.True(Up(Utc(2026, 6, 21, 19, 50)));
        Assert.False(Up(Utc(2026, 6, 21, 20, 5)));
    }

    [Fact]
    public void Nuit_et_jour_polaires()
    {
        Assert.False(Up(Utc(2026, 12, 21, 11, 0), 69.65, 18.96));   // Tromsø : nuit polaire, même à midi
        Assert.True(Up(Utc(2026, 6, 21, 23, 0), 69.65, 18.96));     // soleil de minuit
    }
}

public class AutoThemeTests
{
    static LauncherConfig Paris()
    {
        var cfg = DefaultConfig.Create();
        cfg.Place = "Paris";
        cfg.Latitude = 48.8566;
        cfg.Longitude = 2.3522;
        return cfg;
    }

    static DateTimeOffset Local(int h, int min = 0) => new DateTimeOffset(2026, 10, 28, h, min, 0, TimeSpan.FromHours(1));

    [Fact]
    public void Clair_le_jour_sombre_le_soir_selon_le_soleil()
    {
        var cfg = Paris();   // 28 octobre à Paris : lever vers 7 h 25, coucher vers 17 h 20
        Assert.Equal("kuri", Themes.Resolve(cfg, Local(7, 5)).Id);
        Assert.Equal("hinoki", Themes.Resolve(cfg, Local(7, 45)).Id);
        Assert.Equal("hinoki", Themes.Resolve(cfg, Local(17, 0)).Id);
        Assert.Equal("kuri", Themes.Resolve(cfg, Local(17, 40)).Id);
    }

    [Fact]
    public void Sans_lieu_jour_de_7_h_a_19_h()
    {
        var cfg = DefaultConfig.Create();
        Assert.False(cfg.HasPlace);
        Assert.Equal("kuri", Themes.Resolve(cfg, Local(6, 59)).Id);
        Assert.Equal("hinoki", Themes.Resolve(cfg, Local(7)).Id);
        Assert.Equal("hinoki", Themes.Resolve(cfg, Local(18, 59)).Id);
        Assert.Equal("kuri", Themes.Resolve(cfg, Local(19)).Id);
    }

    [Fact]
    public void Theme_fixe_ignore_l_heure()
    {
        var cfg = Paris();
        cfg.Theme = "sumi";
        Assert.Equal("sumi", Themes.Resolve(cfg, Local(12)).Id);
        Assert.Equal("Sumi", Themes.Describe(cfg));
    }

    [Fact]
    public void Bouton_theme_passe_par_automatique()
    {
        Assert.Equal("kuri", Themes.Next(Themes.Auto));
        Assert.Equal(Themes.Auto, Themes.Next("hinoki"));
        Assert.Equal(Themes.Auto, Themes.Next("inconnu"));
        Assert.Equal("Automatique (jour : Hinoki, soir : Kuri · noyer)", Themes.Describe(DefaultConfig.Create()));
        Assert.True(Themes.IsLight(Themes.Get("hinoki")));
        Assert.True(Themes.IsLight(Themes.Get("japandi")));
        Assert.False(Themes.IsLight(Themes.Get("kuri")));
        Assert.False(Themes.IsLight(Themes.Get("sumi")));
    }

    [Fact]
    public void Migration_v5_le_theme_choisi_garde_son_moment()
    {
        var dark = ConfigStore.Normalize(ConfigStore.Deserialize<LauncherConfig>("{\"Version\":4,\"Theme\":\"fume\"}"));
        Assert.Equal(Themes.Auto, dark.Theme);
        Assert.Equal("fume", dark.NightTheme);
        Assert.Equal("hinoki", dark.DayTheme);
        Assert.Equal(LauncherConfig.WeatherOn, dark.Weather);
        var light = ConfigStore.Normalize(ConfigStore.Deserialize<LauncherConfig>("{\"Version\":4,\"Theme\":\"japandi\"}"));
        Assert.Equal("japandi", light.DayTheme);
        Assert.Equal("kuri", light.NightTheme);
        // Une fois en v5, un thème fixe choisi au bouton reste fixe.
        Assert.Equal("sumi", ConfigStore.Normalize(ConfigStore.Deserialize<LauncherConfig>("{\"Version\":5,\"Theme\":\"sumi\"}")).Theme);
    }
}

public class LocalOverridesTests
{
    static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "qh-" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void Le_lieu_vient_de_local_json_et_survit_a_la_reinitialisation()
    {
        var dir = TempDir();
        File.WriteAllText(Path.Combine(dir, "local.json"), "{\"Place\":\"Lyon\",\"Latitude\":45.76,\"Longitude\":4.84}");
        var store = new ConfigStore(Path.Combine(dir, "config.json")) { Overrides = ConfigStore.ReadOverrides(Path.Combine(dir, "local.json")) };
        var cfg = store.Load();
        Assert.True(cfg.HasPlace);
        Assert.Equal("Lyon", cfg.Place);
        Assert.Equal(45.76, cfg.Latitude);
        Assert.True(store.Reset().HasPlace);
        // Enregistré puis relu : le lieu tient aussi sans local.json.
        Assert.True(store.Save(cfg));
        Assert.Equal(4.84, new ConfigStore(Path.Combine(dir, "config.json")).Load().Longitude);
    }

    [Fact]
    public void Lieu_absent_invalide_ou_fichier_illisible()
    {
        var dir = TempDir();
        Assert.Null(ConfigStore.ReadOverrides(Path.Combine(dir, "absent.json")));
        File.WriteAllText(Path.Combine(dir, "bad.json"), "{pas du json");
        Assert.Null(ConfigStore.ReadOverrides(Path.Combine(dir, "bad.json")));
        Assert.False(new ConfigStore(Path.Combine(dir, "config.json")).Load().HasPlace);   // pas de météo par défaut
        var store = new ConfigStore(Path.Combine(dir, "c2.json")) { Overrides = new LocalOverrides { Latitude = 120, Longitude = 4 } };
        Assert.False(store.Load().HasPlace);
    }
}

public class WeatherTests
{
    // Extrait d'une réponse réelle d'Open-Meteo (Paris, 28 octobre 2026, timeformat=unixtime), avec une valeur nulle.
    const string Sample = "{\"latitude\":48.853382,\"longitude\":2.355208,\"utc_offset_seconds\":0,\"timezone\":\"GMT\"," +
        "\"hourly_units\":{\"time\":\"unixtime\",\"temperature_2m\":\"°C\",\"weather_code\":\"wmo code\",\"is_day\":\"\"}," +
        "\"hourly\":{\"time\":[1793145600,1793149200,1793152800,1793156400]," +
        "\"temperature_2m\":[10.2,9.6,null,-0.4],\"weather_code\":[0,2,3,61],\"is_day\":[0,0,1,1]}}";

    [Fact]
    public void Lit_la_reponse_Open_Meteo()
    {
        var hours = Weather.Parse(Sample);
        Assert.Equal(3, hours.Count);                       // l'heure sans température est ignorée
        Assert.Equal(new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc), hours[0].Utc);
        Assert.Equal(10.2, hours[0].Temperature);
        Assert.False(hours[0].IsDay);
        Assert.Equal(61, hours[2].Code);
        Assert.True(hours[2].IsDay);
        Assert.Empty(Weather.Parse("{pas du json"));
        Assert.Empty(Weather.Parse("{\"error\":true,\"reason\":\"Latitude must be in range\"}"));
    }

    [Fact]
    public void Prochaines_heures_a_partir_de_l_heure_en_cours()
    {
        var t0 = new DateTime(2026, 10, 28, 20, 0, 0, DateTimeKind.Utc);
        var all = Enumerable.Range(0, 48).Select(i => new HourForecast { Utc = t0.AddHours(i - 5) }).Reverse().ToList();
        var next = Weather.Upcoming(all, t0.AddMinutes(47));
        Assert.Equal(24, next.Count);
        Assert.Equal(t0, next[0].Utc);                      // 20 h 47 : on part de 20 h
        Assert.Equal(t0.AddHours(23), next[23].Utc);
        Assert.Equal(19, Weather.Upcoming(all, t0.AddHours(24)).Count);   // fin des données : moins de 24 h
    }

    [Theory]
    [InlineData(0, true, "weather-sun")]
    [InlineData(0, false, "weather-moon")]
    [InlineData(2, true, "weather-sun-cloud")]
    [InlineData(2, false, "weather-moon-cloud")]
    [InlineData(3, true, "weather-cloud")]
    [InlineData(45, true, "weather-fog")]
    [InlineData(53, true, "weather-drizzle")]
    [InlineData(63, false, "weather-rain")]
    [InlineData(81, true, "weather-rain")]
    [InlineData(75, true, "weather-snow")]
    [InlineData(95, true, "weather-storm")]
    [InlineData(7, true, "weather-cloud")]
    public void Pictogrammes_des_codes_WMO(int code, bool day, string icon) => Assert.Equal(icon, Weather.Icon(code, day));

    [Fact]
    public void Pictogrammes_presents_dans_les_ressources()
    {
        var icons = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "res", "icons");
        foreach (var code in new[] { 0, 2, 3, 45, 53, 63, 75, 95 })
            foreach (var day in new[] { true, false })
                Assert.True(File.Exists(Path.Combine(icons, Weather.Icon(code, day) + ".png")), Weather.Icon(code, day));
    }

    [Fact]
    public void Libelles_temperatures_et_requete()
    {
        var fr = new CultureInfo("fr-FR");
        Assert.Equal("Maint.", Weather.HourLabel(new DateTime(2026, 10, 28, 21, 0, 0), true, fr));
        Assert.Equal("22h", Weather.HourLabel(new DateTime(2026, 10, 28, 22, 0, 0), false, fr));
        Assert.Equal("jeu.", Weather.HourLabel(new DateTime(2026, 10, 29, 0, 0, 0), false, fr));
        Assert.Equal("0°", Weather.Degrees(-0.4));
        Assert.Equal("-3°", Weather.Degrees(-2.5));
        Assert.Equal("15°", Weather.Degrees(14.5));
        var url = Weather.ForecastUrl(48.85661, 2.35222);
        Assert.StartsWith("https://api.open-meteo.com/v1/forecast?latitude=48.8566&longitude=2.3522&", url);
        Assert.Contains("forecast_hours=48", url);
    }

    [Fact]
    public void Cache_valable_pour_son_lieu()
    {
        var json = ConfigStore.Serialize(new WeatherCache { Latitude = 48.8566, Longitude = 2.3522, FetchedUnix = 1793145600, Response = Sample });
        var cache = ConfigStore.Deserialize<WeatherCache>(json);
        Assert.True(cache.IsFor(48.8566, 2.3522));
        Assert.False(cache.IsFor(45.76, 4.84));
        Assert.Equal(3, Weather.Parse(cache.Response).Count);
    }
}
