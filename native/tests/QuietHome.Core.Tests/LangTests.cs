using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using QuietHome.Core;
using Xunit;

// Lang.Current est global : les tests ne tournent pas en parallèle, et chaque test anglais remet le français.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

public class LangTests
{
    [Theory]
    [InlineData("auto", "fr_FR", "fr")]
    [InlineData("auto", "fr-BE", "fr")]
    [InlineData("auto", "en_GB", "en")]
    [InlineData("auto", "de_DE", "en")]
    [InlineData("auto", "", "en")]
    [InlineData("fr", "en_US", "fr")]
    [InlineData("en", "fr_FR", "en")]
    public void Langue_choisie_ou_celle_de_la_TV(string setting, string locale, string expected) =>
        Assert.Equal(expected, Lang.Resolve(setting, locale));

    [Fact]
    public void Reglage_en_boucle()
    {
        Assert.Equal("fr", Lang.Next("auto", 1));
        Assert.Equal("en", Lang.Next("fr", 1));
        Assert.Equal("auto", Lang.Next("en", 1));
        Assert.Equal("en", Lang.Next("auto", -1));
        Assert.Equal("fr", Lang.Next("inconnu", 1));
    }

    [Fact]
    public void Langue_inconnue_ramenee_a_auto()
    {
        var cfg = DefaultConfig.Create();
        Assert.Equal(Lang.Auto, cfg.Language);
        cfg.Language = "de";
        Assert.Equal(Lang.Auto, ConfigStore.Normalize(cfg).Language);
        cfg.Language = null;
        Assert.Equal(Lang.Auto, ConfigStore.Normalize(cfg).Language);
        cfg.Language = "en";
        Assert.Equal("en", ConfigStore.Normalize(cfg).Language);
    }

    [Fact]
    public void Table_anglaise_complete_et_coherente()
    {
        foreach (var pair in Lang.EnglishTable)
        {
            Assert.False(string.IsNullOrWhiteSpace(pair.Value), pair.Key);
            // Mêmes trous {0}, {1}… des deux côtés.
            var holes = new Func<string, string>(s => string.Join(",", Regex.Matches(s, @"\{\d\}").Cast<Match>().Select(m => m.Value).OrderBy(v => v)));
            Assert.Equal(holes(pair.Key), holes(pair.Value));
        }
        Assert.Equal("Settings", Lang.T("Réglages", Lang.English));
        Assert.Equal("Réglages", Lang.T("Réglages", Lang.French));
        Assert.Equal("Netflix", Lang.T("Netflix", Lang.English));
    }

    [Fact]
    public void Accueil_en_anglais()
    {
        try
        {
            Lang.Use(Lang.English, "");
            var cfg = DefaultConfig.Create();
            cfg = HdmiPorts.Set(cfg, 1, "decodeur");
            var rows = Catalog.BuildRows(new System.Collections.Generic.List<AppEntry>(), cfg);
            Assert.Equal(new[] { "Theme", "Settings" }, rows.First(r => r.Id == Catalog.RowQuick).Tiles.Select(t => t.Label));
            Assert.Equal("Apps", rows.First(r => r.Id == Catalog.RowFavorites).Title);
            var inputs = rows.First(r => r.Id == Catalog.RowInputs);
            Assert.Equal("Devices", inputs.Title);
            Assert.Equal("TV box", inputs.Tiles[0].Label);
            Assert.Equal("Automatic (day: Hinoki, evening: Kuri · walnut)", Themes.Describe(DefaultConfig.Create()));
            Assert.Equal("Good morning", Themes.Greeting(9));
            Assert.Equal("Good afternoon", Themes.Greeting(15));
            Assert.Equal("Good evening", Themes.Greeting(20));
            Assert.Equal("Good night", Themes.Greeting(2));
            Assert.Equal(Themes.ToneFor("Toutes les apps").From, Themes.ToneFor("All apps").From);
        }
        finally
        {
            Lang.Use(Lang.French, "");
        }
        Assert.Equal("Bonjour", Themes.Greeting(15));
    }

    [Fact]
    public void Meteo_en_anglais()
    {
        var en = Lang.CultureFor(Lang.English);
        Assert.Equal("Now", Weather.HourLabel(new DateTime(2026, 10, 28, 21, 0, 0), true, en));
        Assert.Equal("22:00", Weather.HourLabel(new DateTime(2026, 10, 28, 22, 0, 0), false, en));
        Assert.Equal("10pm", Weather.HourLabel(new DateTime(2026, 10, 28, 22, 0, 0), false, en, clock24h: false));
        Assert.Equal("12pm", Weather.HourLabel(new DateTime(2026, 10, 28, 12, 0, 0), false, en, clock24h: false));
        Assert.Equal("Thu", Weather.HourLabel(new DateTime(2026, 10, 29, 0, 0, 0), false, en));
        Assert.Equal("Wednesday 28 October", new DateTime(2026, 10, 28).ToString("dddd d MMMM", en));
    }
}
