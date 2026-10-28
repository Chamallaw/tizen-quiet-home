using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuietHome.Core;
using Xunit;

public class CatalogTests
{
    static readonly List<AppEntry> Apps = new()
    {
        new("org.tizen.netflix-app", "Netflix"),
        new("NewId.Disney", "Disney+"),               // ID inconnu, résolu par le nom
        new("aLlph042JZ.arte", "ARTE TV"),
        new("org.tizen.browser", "Browser"),          // préfixe masqué mais toujours visible
        new("org.tizen.tv-viewer", "TV"),              // système : masqué
        new("com.samsung.tv.store", "Apps"),
        new("hidden.app", "Caché", noDisplay: true),
    };

    static Row RowOf(List<Row> rows, string id) => rows.Single(r => r.Id == id);

    [Fact]
    public void Favoris_resolus_par_id_puis_par_nom_et_grises_si_absents()
    {
        var fav = RowOf(Catalog.BuildRows(Apps, DefaultConfig.Create()), Catalog.RowFavorites).Tiles;
        Assert.Equal(new[] { "Netflix", "YouTube", "Jellyfin", "Spotify", "Prime Video", "Disney+", "Free TV", "Toutes les apps" },
            fav.Select(t => t.Label));
        Assert.Equal("org.tizen.netflix-app", fav[0].AppId);
        Assert.Equal("res:icons/netflix.png", fav[0].IconPath);
        Assert.Equal("res:icons/youtube.png", fav[1].IconPath);
        Assert.Equal("res:icons/jellyfin.png", fav[2].IconPath);   // logo présent même si l'app manque
        Assert.True(fav[2].Missing);                   // Jellyfin absent de la liste de test
        Assert.Equal("NewId.Disney", fav[5].AppId);    // résolu par le nom
        Assert.False(fav[5].Missing);
    }

    [Fact]
    public void Bibliotheque_filtre_systeme_favoris_et_nodisplay()
    {
        var cfg = DefaultConfig.Create();
        cfg.ShowLibrary = true;
        var lib = RowOf(Catalog.BuildRows(Apps, cfg), Catalog.RowLibrary).Tiles.Select(t => t.AppId).ToList();
        Assert.Equal(new[] { "aLlph042JZ.arte", "org.tizen.browser" }, lib);
    }

    [Fact]
    public void Epure_par_defaut_toutes_les_apps_dans_les_reglages()
    {
        var cfg = DefaultConfig.Create();
        var rows = Catalog.BuildRows(Apps, cfg);
        Assert.DoesNotContain(rows, r => r.Id == Catalog.RowLibrary);
        var last = RowOf(rows, Catalog.RowFavorites).Tiles.Last();
        Assert.Equal(Catalog.ActAllApps, last.Action);
        Assert.False(last.Pinned);   // ne se déplace pas avec les favoris
        Assert.Equal("res:icons/apps.png", last.IconPath);
        var all = Catalog.VisibleApps(Apps, cfg).Select(a => a.Id).ToList();
        Assert.Equal(new[] { "aLlph042JZ.arte", "org.tizen.browser", "NewId.Disney", "org.tizen.netflix-app" }, all);
    }

    [Fact]
    public void Tuile_reglages_toujours_en_tete()
    {
        var rows = Catalog.BuildRows(new List<AppEntry>(), DefaultConfig.Create());
        Assert.Equal(Catalog.RowQuick, rows[0].Id);
        Assert.Equal(new[] { Catalog.ActCycleTheme, Catalog.ActMenu }, rows[0].Tiles.Select(t => t.Action));
        Assert.Equal("res:icons/theme.png", rows[0].Tiles[0].IconPath);
        var menu = rows[0].Tiles[1];
        Assert.Equal("res:icons/settings.png", menu.IconPath);
        Assert.Equal("Applications", rows[1].Title);
    }

    [Fact]
    public void TogglePin_retire_un_favori_resolu_par_nom_sans_doublon()
    {
        var cfg = DefaultConfig.Create();
        var disney = RowOf(Catalog.BuildRows(Apps, cfg), Catalog.RowFavorites).Tiles[5];
        var next = Catalog.TogglePin(cfg, disney);
        Assert.Equal(cfg.Pinned.Count - 1, next.Pinned.Count);
        Assert.DoesNotContain(next.Pinned, p => p.Name == "Disney+");
        Assert.Equal(7, cfg.Pinned.Count); // l'original n'est pas muté
    }

    [Fact]
    public void Epingler_puis_masquer_une_app_de_la_bibliotheque()
    {
        var cfg = DefaultConfig.Create();
        cfg.ShowLibrary = true;
        var arte = RowOf(Catalog.BuildRows(Apps, cfg), Catalog.RowLibrary).Tiles[0];
        var pinned = Catalog.TogglePin(cfg, arte);
        Assert.Equal("aLlph042JZ.arte", pinned.Pinned.Last().AppIds[0]);
        var hidden = Catalog.Hide(pinned, new Tile { AppId = "aLlph042JZ.arte" });
        Assert.Contains("aLlph042JZ.arte", hidden.Hidden);
        Assert.DoesNotContain(hidden.Pinned, p => p.AppIds.Contains("aLlph042JZ.arte"));
        Assert.Empty(Catalog.UnhideAll(hidden).Hidden);
    }

    [Fact]
    public void MovePin_echange_et_reste_borne()
    {
        var cfg = DefaultConfig.Create();
        var first = RowOf(Catalog.BuildRows(Apps, cfg), Catalog.RowFavorites).Tiles[0];
        Assert.Equal("Netflix", Catalog.MovePin(cfg, first, -1).Pinned[0].Name);
        Assert.Equal("Netflix", Catalog.MovePin(cfg, first, 1).Pinned[1].Name);
    }

    [Fact]
    public void Grille_toutes_les_apps_logos_et_favoris()
    {
        var apps = new List<AppEntry>(Apps) { new("kIciSQlYEM.plex", "Plex"), new("x.twitch", "Twitch") };
        var tiles = Catalog.AllAppTiles(apps, DefaultConfig.Create());
        var netflix = tiles.Single(t => t.AppId == "org.tizen.netflix-app");
        Assert.True(netflix.Pinned);
        Assert.Equal(0, netflix.PinIndex);
        var disney = tiles.Single(t => t.AppId == "NewId.Disney");   // favori résolu par le nom
        Assert.True(disney.Pinned);
        Assert.Equal(5, disney.PinIndex);
        Assert.Equal("res:icons/plex.png", tiles.Single(t => t.Label == "Plex").IconPath);
        Assert.Equal("res:icons/twitch.png", tiles.Single(t => t.Label == "Twitch").IconPath);
        Assert.False(tiles.Single(t => t.Label == "ARTE TV").Pinned);
        Assert.DoesNotContain(tiles, t => t.AppId == "org.tizen.tv-viewer");
    }

    [Fact]
    public void Signature_stable_et_sensible_aux_changements()
    {
        var a = Catalog.Signature(Apps);
        Assert.Equal(a, Catalog.Signature(Apps.AsEnumerable().Reverse().ToList()));   // ordre indifférent
        Assert.NotEqual(a, Catalog.Signature(Apps.Skip(1).ToList()));
    }

    [Fact]
    public void Nouvelles_marques()
    {
        Assert.Equal("#ebaf00", Themes.BrandFor("Plex").MarkColor);
        Assert.Equal("appletv", Themes.BrandFor("Apple TV").Mark);
        Assert.Equal("nvidia", Themes.BrandFor("GeForce NOW").Mark);
        Assert.Equal("xbox", Themes.BrandFor("Xbox").Mark);
    }

    [Fact]
    public void Consoles_PS5_HDMI2_et_Switch_HDMI4()
    {
        var cfg = DefaultConfig.Create();
        var inputs = RowOf(Catalog.BuildRows(Apps, cfg), Catalog.RowInputs).Tiles;
        Assert.Equal(new[] { "PS5", "Switch" }, inputs.Select(t => t.Label));
        Assert.Equal(new[] { "14", "16" }, inputs.Select(t => t.Action));
        Assert.Equal(new[] { "res:icons/playstation.png", "res:icons/nintendoswitch.png" }, inputs.Select(t => t.IconPath));
        Assert.Equal("playstation", Catalog.InputIcon(new InputSpec("PlayStation", 13)));
        Assert.Equal("nintendoswitch", Catalog.InputIcon(new InputSpec("Console", 16, "switch")));  // config v2
        Assert.Null(Catalog.InputIcon(new InputSpec("Box", 15)));
        cfg.Inputs[1].Enabled = false;
        Assert.Single(RowOf(Catalog.BuildRows(Apps, cfg), Catalog.RowInputs).Tiles);
    }

    [Fact]
    public void Entrees_HDMI_reglees_dans_l_app()
    {
        var cfg = DefaultConfig.Create();
        Assert.Equal("Rien", HdmiPorts.At(cfg, 1).Label);
        Assert.Equal("ps5", HdmiPorts.At(cfg, 2).Key);
        Assert.Equal("switch", HdmiPorts.At(cfg, 4).Key);

        // Décodeur sur HDMI 1, barre de son (avec entrée HDMI) sur HDMI 3 : chacun sa tuile, la rangée devient « Appareils ».
        cfg = HdmiPorts.Set(HdmiPorts.Set(cfg, 1, "decodeur"), 3, "barre");
        var row = RowOf(Catalog.BuildRows(Apps, cfg), Catalog.RowInputs);
        Assert.Equal("Appareils", row.Title);
        Assert.Equal(new[] { "Décodeur TV", "PS5", "Barre de son", "Switch" }, row.Tiles.Select(t => t.Label));
        Assert.Equal(new[] { "13", "14", "15", "16" }, row.Tiles.Select(t => t.Action));
        Assert.Equal("res:icons/device-barre.png", row.Tiles[2].IconPath);
        Assert.Equal("Barre de son", HdmiPorts.At(cfg, 3).Label);

        // La PS5 change de prise : HDMI 2 vidé, HDMI 3 remplacé.
        cfg = HdmiPorts.Set(HdmiPorts.Set(cfg, 2, HdmiPorts.None), 3, "ps5");
        Assert.Equal("Rien", HdmiPorts.At(cfg, 2).Label);
        Assert.Equal(new[] { 13, 15, 16 }, cfg.Inputs.Select(i => i.SourceId));
        Assert.Equal("res:icons/playstation.png", RowOf(Catalog.BuildRows(Apps, cfg), Catalog.RowInputs).Tiles[1].IconPath);
        foreach (var kind in HdmiPorts.Kinds.Where(k => k.Icon != null))   // chaque logo ou pictogramme existe
            Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "res", "icons", kind.Icon + ".png")), kind.Icon);

        // Que des consoles : « Consoles ».
        cfg = HdmiPorts.Set(cfg, 1, HdmiPorts.None);
        Assert.Equal("Consoles", RowOf(Catalog.BuildRows(Apps, cfg), Catalog.RowInputs).Title);

        // ◀ ▶ : Rien → PS5 → … → Appareil → Rien.
        Assert.Equal("ps5", HdmiPorts.Next(HdmiPorts.None, 1));
        Assert.Equal(HdmiPorts.None, HdmiPorts.Next("autre", 1));
        Assert.Equal("autre", HdmiPorts.Next(HdmiPorts.None, -1));
    }

    [Fact]
    public void Entrees_d_avant_le_reglage_reconnues_par_leur_nom()
    {
        var cfg = ConfigStore.Normalize(ConfigStore.Deserialize<LauncherConfig>(
            "{\"Version\":5,\"Inputs\":[{\"Label\":\"PS5\",\"SourceId\":14,\"Enabled\":true},{\"Label\":\"Console\",\"SourceId\":16,\"Enabled\":true,\"Icon\":\"switch\"}]}"));
        Assert.Equal("ps5", HdmiPorts.At(cfg, 2).Key);
        Assert.Equal("switch", HdmiPorts.At(cfg, 4).Key);
        Assert.Equal("Consoles", RowOf(Catalog.BuildRows(Apps, cfg), Catalog.RowInputs).Title);
    }
}

public class FocusTests
{
    static List<Row> Rows(params int[] counts) =>
        counts.Select((n, i) => new Row { Id = "r" + i, Tiles = Enumerable.Range(0, n).Select(c => new Tile { Key = i + ":" + c }).ToList() }).ToList();

    [Fact]
    public void Memorise_la_colonne_et_saute_les_rangees_vides()
    {
        var f = new FocusModel();
        f.SetRows(Rows(3, 0, 2));
        Assert.True(f.Move(Direction.Right));
        Assert.True(f.Move(Direction.Right));
        Assert.False(f.Move(Direction.Right));
        Assert.Equal("0:2", f.Current.Key);
        Assert.True(f.Move(Direction.Down));
        Assert.Equal(2, f.RowIndex);
        Assert.Equal("2:0", f.Current.Key);
        Assert.False(f.Move(Direction.Down));
        Assert.True(f.Move(Direction.Up));
        Assert.Equal("0:2", f.Current.Key);
        Assert.False(f.Move(Direction.Up));
    }

    [Fact]
    public void SetRows_recadre_apres_suppression()
    {
        var f = new FocusModel();
        f.SetRows(Rows(5));
        for (int i = 0; i < 4; i++) f.Move(Direction.Right);
        f.SetRows(Rows(2));
        Assert.Equal("0:1", f.Current.Key);
    }

    [Fact]
    public void Demarre_sur_la_rangee_d_accueil_puis_monte_vers_les_reglages()
    {
        var rows = Rows(1, 4, 2);
        rows[1].Id = "home";
        var f = new FocusModel { HomeRowId = "home" };
        f.SetRows(rows);
        Assert.Equal(1, f.RowIndex);
        f.Move(Direction.Right);
        Assert.True(f.Move(Direction.Up));
        Assert.Equal(0, f.RowIndex);
        f.Move(Direction.Down);
        Assert.Equal("1:1", f.Current.Key);   // colonne mémorisée
        f.Reset();
        Assert.Equal("1:0", f.Current.Key);
    }

    [Fact]
    public void Retour_sur_la_tuile_de_l_app_quittee()
    {
        var rows = Rows(2, 4, 2);
        rows[1].Id = "home";
        rows[1].Tiles[2].AppId = "disney";
        var f = new FocusModel { HomeRowId = "home" };
        f.SetRows(rows);
        f.Reset();
        Assert.True(f.FocusTile(t => t.AppId == "disney"));
        Assert.Equal("1:2", f.Current.Key);
        Assert.False(f.FocusTile(t => t.AppId == "absente"));
        Assert.Equal("1:2", f.Current.Key);                 // focus inchangé
    }

    [Fact]
    public void Aucune_rangee_ne_plante_pas()
    {
        var f = new FocusModel();
        f.SetRows(new List<Row>());
        Assert.Null(f.Current);
        Assert.False(f.Move(Direction.Down));
    }
}

public class ConfigStoreTests
{
    static string TempPath() => Path.Combine(Path.GetTempPath(), "qh-" + Path.GetRandomFileName(), "config.json");

    [Fact]
    public void Aller_retour_et_bak()
    {
        var path = TempPath();
        var store = new ConfigStore(path);
        var cfg = DefaultConfig.Create();
        cfg.Theme = "sumi";
        Assert.True(store.Save(cfg));
        cfg.Theme = "hinoki";
        Assert.True(store.Save(cfg));
        Assert.Equal("hinoki", store.Load().Theme);
        Assert.True(File.Exists(path + ".bak"));
    }

    [Fact]
    public void Fichier_corrompu_retombe_sur_bak_puis_defauts()
    {
        var path = TempPath();
        var store = new ConfigStore(path);
        var cfg = DefaultConfig.Create();
        cfg.Theme = "sumi";
        store.Save(cfg);
        store.Save(cfg);
        File.WriteAllText(path, "{pas du json");
        Assert.Equal("sumi", store.Load().Theme);
        File.WriteAllText(path + ".bak", "{");
        Assert.Equal(Themes.Auto, store.Load().Theme);
        Assert.NotNull(store.LastError);
    }

    [Fact]
    public void Champs_absents_completes()
    {
        var cfg = ConfigStore.Normalize(ConfigStore.Deserialize<LauncherConfig>("{\"Version\":2,\"Theme\":\"sumi\"}"));
        Assert.Equal(Themes.Auto, cfg.Theme);
        Assert.Equal("sumi", cfg.NightTheme);
        Assert.NotEmpty(cfg.Inputs);
        Assert.NotEmpty(cfg.AlwaysVisible);
        Assert.Equal("com.samsung.tv.csfs", cfg.SamsungHomeAppId);
    }

    [Fact]
    public void Soften_desature_et_assombrit_sans_changer_la_teinte()
    {
        Assert.Equal("#000000", Themes.Soften("#000000"));
        Assert.Equal("#d1d1d1", Themes.Soften("#ffffff"));           // gris : seule la luminosité baisse
        var red = Themes.ParseColor(Themes.Soften("#e50914"));
        Assert.True(red[0] > red[1] * 4 && red[0] < 0.9f);           // toujours rouge, moins éclatant
    }

    [Theory]
    [InlineData(8, "Bonjour")]
    [InlineData(17, "Bonjour")]
    [InlineData(18, "Bonsoir")]
    [InlineData(23, "Bonne nuit")]
    [InlineData(3, "Bonne nuit")]
    public void Salutation_selon_l_heure(int hour, string expected) => Assert.Equal(expected, Themes.Greeting(hour));

    [Fact]
    public void Theme_choisi_persiste_apres_rechargement()
    {
        var path = TempPath();
        var store = new ConfigStore(path);
        var cfg = store.Load();
        for (int i = 0; i < 3; i++)
        {
            cfg.Theme = Themes.Next(cfg.Theme);
            Assert.True(store.Save(cfg));                            // remplacement d'un fichier existant
        }
        Assert.Equal("japandi", new ConfigStore(path).Load().Theme);   // automatique → kuri → fume → japandi
    }

    [Fact]
    public void Config_v1_migree_vers_les_defauts_v2()
    {
        var path = TempPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{\"Version\":1,\"Theme\":\"nuit\",\"DevLogHost\":\"10.0.0.5\",\"Pinned\":[]}");
        var cfg = new ConfigStore(path).Load();
        Assert.Equal(LauncherConfig.CurrentVersion, cfg.Version);
        Assert.Equal(Themes.Auto, cfg.Theme);
        Assert.Equal(7, cfg.Pinned.Count);
        Assert.Equal("10.0.0.5", cfg.DevLogHost);
    }

    [Fact]
    public void Config_v2_recoit_Free_TV_sans_perdre_ses_reglages()
    {
        var path = TempPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{\"Version\":2,\"Theme\":\"sumi\",\"Pinned\":[" +
            "{\"Name\":\"Netflix\",\"AppIds\":[\"org.tizen.netflix-app\"]}," +
            "{\"Name\":\"Plex\",\"AppIds\":[\"kIciSQlYEM.plex\"]}]}");
        var cfg = new ConfigStore(path).Load();
        Assert.Equal("sumi", cfg.NightTheme);                             // réglages conservés (thème du soir)
        Assert.Equal(new[] { "Netflix", "YouTube", "Plex", "Free TV" }, cfg.Pinned.Select(p => p.Name));
        Assert.Equal(LauncherConfig.CurrentVersion, cfg.Version);
        // Déjà épinglée (autre nom) : pas de doublon.
        File.WriteAllText(path, "{\"Version\":3,\"Pinned\":[{\"Name\":\"Oqee\",\"AppIds\":[\"znCs95n93Q.OqeeTV\"]}]}");
        Assert.Equal(new[] { "YouTube", "Oqee" }, new ConfigStore(path).Load().Pinned.Select(p => p.Name));
    }

    [Fact]
    public void Config_v3_recoit_YouTube_apres_Netflix_sans_doublon()
    {
        var path = TempPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{\"Version\":3,\"Theme\":\"fume\",\"Pinned\":[" +
            "{\"Name\":\"Netflix\",\"AppIds\":[\"org.tizen.netflix-app\"]}," +
            "{\"Name\":\"Spotify\",\"AppIds\":[\"rJeHak5zRg.Spotify\"]}]}");
        var cfg = new ConfigStore(path).Load();
        Assert.Equal("fume", cfg.NightTheme);
        Assert.Equal(new[] { "Netflix", "YouTube", "Spotify" }, cfg.Pinned.Select(p => p.Name));
        Assert.Equal("xvvl3S1TT1.TizenTubeStandalone", cfg.Pinned[1].AppIds[0]);
        // TizenTube déjà épinglé sous un autre nom : pas de doublon.
        File.WriteAllText(path, "{\"Version\":3,\"Pinned\":[{\"Name\":\"TizenTube\",\"AppIds\":[\"xvvl3S1TT1.TizenTubeStandalone\"]}]}");
        Assert.Single(new ConfigStore(path).Load().Pinned);
        Assert.Equal("youtube", Themes.BrandFor("TizenTube").Mark);   // logo YouTube aussi dans la grille
    }

    [Fact]
    public void Mode_ambiant_par_defaut_et_desactivable()
    {
        Assert.Equal(5, ConfigStore.Normalize(ConfigStore.Deserialize<LauncherConfig>("{\"Version\":2}")).AmbientMinutes);
        Assert.Equal(-1, ConfigStore.Normalize(ConfigStore.Deserialize<LauncherConfig>("{\"Version\":2,\"AmbientMinutes\":-1}")).AmbientMinutes);
    }

    [Fact]
    public void Theme_suivant_et_couleurs()
    {
        Assert.Equal(new[] { "kuri", "fume", "japandi", "sumi", "hinoki" }, Themes.All.Select(t => t.Id));
        Assert.Equal("fume", Themes.Next("kuri"));
        Assert.Equal(Themes.Auto, Themes.Next("hinoki"));
        Assert.Equal("kuri", Themes.Get("inconnu").Id);
        Assert.Equal("res:wallpapers/kuri.jpg", Themes.Get("kuri").Wallpaper);
        Assert.True(Themes.Get("kuri").SoftenTiles);
        Assert.False(Themes.Get("japandi").SoftenTiles);
        Assert.Equal("#e50914", Themes.BrandFor("Netflix").MarkColor);  // logo rouge sur noir
        Assert.Equal("#1ed760", Themes.BrandFor("Spotify").MarkColor);
        Assert.Equal("#00205c", Themes.ToneFor("PS5").From);           // bleu PlayStation
        Assert.Equal("#b3000e", Themes.ToneFor("Switch").From);        // rouge Nintendo
        Assert.Equal("primevideo", Themes.BrandFor("Prime Video").Mark);
        Assert.Null(Themes.BrandFor("Arte"));
        Assert.Same(Themes.ToneFor("Arte"), Themes.ToneFor("ARTE"));  // stable, insensible à la casse
        Assert.Equal(new[] { 1f, 0f, 1f, 1f }, Themes.ParseColor("pas une couleur"));
        Assert.Equal(0.8f, Themes.ParseColor("#000000cc")[3], 2);
    }
}
