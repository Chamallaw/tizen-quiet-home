using System;
using System.Collections.Generic;
using QuietHome.Core;
using Xunit;

public class HomeRedirectTests
{
    const string Self = "QHLauncher.Home", Samsung = HomeRedirect.SamsungHome;
    const string Netflix = "org.tizen.netflix-app", Viewer = "org.tizen.tv-viewer";
    static readonly DateTime T0 = new(2026, 10, 27, 20, 0, 0);

    static DateTime At(double seconds) => T0.AddSeconds(seconds);

    [Fact]
    public void Home_depuis_une_app_ramene_Quiet_Home()
    {
        var r = new HomeRedirect(Self);
        Assert.False(r.Observe(Netflix, At(0)));
        Assert.True(r.Observe(Samsung, At(10)));    // appui sur Home
        Assert.False(r.Observe(Self, At(11)));      // Quiet Home revenu
    }

    [Fact]
    public void Changer_de_source_n_est_jamais_intercepte()
    {
        var r = new HomeRedirect(Self);
        Assert.False(r.Observe(Netflix, At(0)));
        Assert.False(r.Observe(Viewer, At(5)));
        Assert.False(r.Observe(Viewer, At(30)));
    }

    [Fact]
    public void Deux_appuis_rapides_laissent_l_accueil_Samsung()
    {
        var r = new HomeRedirect(Self);
        Assert.True(r.Observe(Samsung, At(0)));
        Assert.False(r.Observe(Self, At(1)));
        Assert.False(r.Observe(Samsung, At(3)));    // 2e appui dans la fenêtre : on laisse
        Assert.False(r.Observe(Samsung, At(20)));   // et ça reste tant qu'on y est
        Assert.False(r.Observe(Netflix, At(30)));   // nouvelle app : l'autorisation est consommée
        Assert.True(r.Observe(Samsung, At(40)));    // Home suivant : de nouveau Quiet Home
    }

    [Fact]
    public void Appui_lent_apres_retour_ramene_encore_Quiet_Home()
    {
        var r = new HomeRedirect(Self);
        Assert.True(r.Observe(Samsung, At(0)));
        Assert.False(r.Observe(Self, At(1)));
        Assert.True(r.Observe(Samsung, At(30)));    // hors fenêtre : simple retour
    }

    [Fact]
    public void Attend_que_Quiet_Home_apparaisse_puis_reessaie()
    {
        var r = new HomeRedirect(Self);
        Assert.True(r.Observe(Samsung, At(0)));
        Assert.False(r.Observe(Samsung, At(1)));    // relevé avant l'arrivée de Quiet Home : ni échappée ni relance
        Assert.False(r.Observe(Samsung, At(2)));
        Assert.True(r.Observe(Samsung, At(3.5)));   // toujours pas là : on relance une fois
    }

    [Fact]
    public void Recuperation_ouvre_l_accueil_Samsung_sans_interception()
    {
        var r = new HomeRedirect(Self);
        Assert.False(r.Observe(Self, At(0)));
        r.AllowSamsungHome();
        Assert.False(r.Observe(Self, At(1)));       // encore Quiet Home : l'autorisation tient
        Assert.False(r.Observe(Samsung, At(2)));
        Assert.False(r.Observe(Samsung, At(15)));
        Assert.False(r.Observe(Self, At(20)));      // retour volontaire à Quiet Home
        Assert.True(r.Observe(Samsung, At(40)));    // Home suivant : interception normale
    }

    [Fact]
    public void Desactive_ne_fait_rien_et_releves_vides_ignores()
    {
        var r = new HomeRedirect(Self) { Enabled = false };
        Assert.False(r.Observe(Samsung, At(0)));
        r.Enabled = true;
        Assert.False(r.Observe(null, At(1)));
        Assert.True(r.Observe(Samsung, At(2)));
    }

    [Fact]
    public void Rallumage_sur_la_derniere_source_ramene_Quiet_Home()
    {
        // Séquence relevée sur la TV : Power depuis Quiet Home, écran vide, rallumage sur le lecteur TV.
        var r = new HomeRedirect(Self);
        Assert.False(r.Observe(Self, At(0)));
        r.NotePowerOff();
        Assert.False(r.Observe(null, At(3)));
        Assert.False(r.Observe(null, At(4)));
        Assert.True(r.Observe(Viewer, At(11)));
        Assert.False(r.Observe(Self, At(13)));
        Assert.False(r.Observe(Viewer, At(60)));   // ensuite, changer de source reste libre
    }

    [Fact]
    public void Ecran_vide_prolonge_vaut_mise_en_veille()
    {
        var r = new HomeRedirect(Self);
        Assert.False(r.Observe(Netflix, At(0)));
        Assert.False(r.Observe(null, At(1)));
        Assert.False(r.Observe(Netflix, At(2)));   // un relevé vide isolé : simple transition
        Assert.False(r.Observe(null, At(3)));
        Assert.False(r.Observe(null, At(4)));      // deux de suite : veille
        Assert.True(r.Observe(Netflix, At(30)));   // reprise de l'app d'avant la veille
    }

    [Fact]
    public void Trou_entre_deux_releves_vaut_mise_en_veille()
    {
        var r = new HomeRedirect(Self);
        Assert.False(r.Observe(Viewer, At(0)));
        Assert.True(r.Observe(Viewer, At(600)));   // processus figé 10 min puis rallumage
    }

    [Fact]
    public void Allumage_par_une_touche_d_app_est_respecte()
    {
        var r = new HomeRedirect(Self);
        Assert.False(r.Observe(Viewer, At(0)));
        r.NotePowerOff();
        Assert.False(r.Observe("org.tizen.primevideo", At(20)));   // touche Prime Video à l'allumage
        Assert.False(r.Observe("org.tizen.primevideo", At(21)));
    }

    [Fact]
    public void Rallumage_ignore_si_desactive()
    {
        var r = new HomeRedirect(Self) { Enabled = false };
        r.NotePowerOff();
        Assert.False(r.Observe(Viewer, At(10)));
    }

    [Fact]
    public void Pick_privilegie_Quiet_Home_puis_l_accueil_Samsung()
    {
        var r = new HomeRedirect(Self);
        Assert.Equal(Self, r.Pick(new List<string> { Samsung, Self }));
        Assert.Equal(Samsung, r.Pick(new List<string> { Netflix, Samsung }));
        Assert.Equal(Viewer, r.Pick(new List<string> { Viewer }));
        Assert.Null(r.Pick(new List<string>()));
    }

    [Fact]
    public void Config_bouton_Home_par_defaut_Quiet_Home()
    {
        Assert.Equal(LauncherConfig.HomeKeyQuietHome,
            ConfigStore.Normalize(ConfigStore.Deserialize<LauncherConfig>("{\"Version\":3}")).HomeKey);
        Assert.Equal(LauncherConfig.HomeKeySamsung,
            ConfigStore.Normalize(ConfigStore.Deserialize<LauncherConfig>("{\"Version\":3,\"HomeKey\":\"samsung\"}")).HomeKey);
    }
}
