"""Maquettes 1920x1080 de Quiet Home, à partir des mêmes valeurs que l'app (HomeScreen, TileView, Themes).

    python -I make_preview_kuri.py [dossier_de_sortie]

Produit, dans native/preview/ par défaut :
- accueil-kuri.html     le soir : mur de noyer, tuiles adoucies, bandeau météo ;
- accueil-hinoki.html   le jour (thème automatique) : dégradé clair, couleurs de marque franches ;
- toutes-les-apps-kuri.html, veille-kuri.html.
À exporter en PNG avec Chrome headless (--window-size=1920,1080 --screenshot).
La météo est un exemple d'automne (températures réelles de Paris, conditions variées pour montrer les pictogrammes).
"""
import base64
import colorsys
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
PT = 1.333  # px par point NUI, approximatif
themes_cs = (ROOT / "src/QuietHome.Core/Themes.cs").read_text(encoding="utf-8")

SAT, LUM = 0.80, 0.82      # adoucissement des couleurs de marque (Themes.Soften)
MARGIN, WEATHER_TOP, HOURS = 80, 880, 24

BRANDS = [(k, m, a, b, c or "#ffffff") for k, m, a, b, c in re.findall(
    r'new Brand\("([^"]+)", "([^"]+)", "(#\w+)", "(#\w+)"(?:, "(#\w+)")?\)', themes_cs)]


def theme(theme_id):
    block = re.search(r'Id = "%s".*?Marker = "[^"]+"' % theme_id, themes_cs, re.S).group(0)
    return dict(re.findall(r'(\w+) = "(#[0-9a-fA-F]+)"', block))


# Exemple : 48 heures à partir du 28 octobre, 1 h (température °C, code WMO, jour).
TEMPS = [10, 10, 9, 9, 8, 8, 8, 8, 8, 10, 12, 13, 14, 15, 15, 15, 15, 14, 13, 13, 12, 11, 10, 10,
         10, 9, 9, 8, 8, 8, 8, 8, 8, 10, 11, 13, 14, 15, 15, 14, 14, 13, 12, 11, 10, 9, 8, 8]
CODES = [0, 0, 0, 0, 0, 0, 45, 45, 2, 1, 2, 2, 3, 3, 2, 2, 3, 61, 61, 80, 3, 2, 1, 0,
         0, 0, 0, 1, 3, 3, 45, 45, 2, 2, 1, 0, 0, 1, 2, 3, 3, 61, 61, 3, 2, 2, 1, 0]
DAY = [0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0,
       0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0]
WEEKDAYS = ["mer.", "jeu.", "ven."]


def weather_icon(code, day):
    """Même table que Weather.Icon."""
    if code == 0:
        return "weather-sun" if day else "weather-moon"
    if code in (1, 2):
        return "weather-sun-cloud" if day else "weather-moon-cloud"
    if code in (45, 48):
        return "weather-fog"
    if code in (51, 53, 55, 56, 57):
        return "weather-drizzle"
    if code in (71, 73, 75, 77, 85, 86):
        return "weather-snow"
    if code in (95, 96, 99):
        return "weather-storm"
    return "weather-rain" if 61 <= code <= 82 else "weather-cloud"


def soften(hex_color, sat=SAT, lum=LUM):
    r, g, b = (int(hex_color[i:i + 2], 16) / 255 for i in (1, 3, 5))
    h, l, s = colorsys.rgb_to_hls(r, g, b)
    r, g, b = colorsys.hls_to_rgb(h, l * lum, s * sat)
    return "#%02x%02x%02x" % (round(r * 255), round(g * 255), round(b * 255))


def brand(label):
    l = label.lower()
    return next((b for b in BRANDS if b[0] in l), None)


def data_uri(path, mime="image/png"):
    return f"data:{mime};base64," + base64.b64encode(pathlib.Path(path).read_bytes()).decode()


def png_size(name):
    head = (ROOT / "res/icons" / f"{name}.png").read_bytes()[:24]
    return int.from_bytes(head[16:20], "big"), int.from_bytes(head[20:24], "big")


def card(x, y, w, h, label, focused=False, subtitle=None, icon_name=None, soft=True):
    """Comme TileView : dégradé de marque (adouci sur le bois), logo centré et teint, sinon pictogramme et nom."""
    b = brand(label)
    if b:
        a, z = (soften(b[2]), soften(b[3])) if soft else (b[2], b[3])
    else:
        a, z = ("#5b4a3c", "#33281f") if soft else ("#8a8178", "#4b453f")
    cls = ("card focus" if focused else "card") + ("" if soft else " flat")
    out = [f'<div class="{cls}" style="left:{x}px;top:{y}px;width:{w}px;height:{h}px;'
           f'background:linear-gradient(135deg,{a},{z})">']
    # Sans fichier de logo, la marque garde son dégradé mais affiche son nom.
    if b and (ROOT / "res/icons" / f"{b[1]}.png").exists():
        iw, ih = png_size(b[1])
        k = min(w * 0.56 / iw, h * (0.40 if subtitle else 0.46) / ih)
        bw, bh = iw * k, ih * k
        cy = h * (0.42 if subtitle else 0.5)
        if soft:
            mark = soften(b[4], 0.9, 0.95) if b[4].lower() != "#ffffff" else "#f3ece2"
        else:
            mark = b[4]
        uri = data_uri(ROOT / "res/icons" / f"{b[1]}.png")
        out.append(f'<div style="position:absolute;left:{(w - bw) / 2}px;top:{cy - bh / 2}px;width:{bw}px;height:{bh}px;'
                   f'background:{mark};-webkit-mask:url({uri}) center/contain no-repeat"></div>')
    else:
        if icon_name:
            uri = data_uri(ROOT / "res/icons" / f"{icon_name}.png")
            out.append(f'<div style="position:absolute;left:18px;top:16px;width:36px;height:36px;background:#efe6d8;'
                       f'-webkit-mask:url({uri}) center/contain no-repeat"></div>')
        out.append(f'<div class="label">{label}</div>')
    if subtitle:
        out.append(f'<div class="sub">{subtitle}</div>')
    out.append("</div>")
    if focused:
        out.append(f'<div class="marker" style="left:{x + (w - 56) / 2}px;top:{y + h + 18}px"></div>')
    return "".join(out)


def weather_strip(t, start, first_local_hour):
    """Bandeau de HomeScreen.UpdateWeather : 24 colonnes heure / pictogramme / température."""
    cell = (1920 - 2 * MARGIN) / HOURS
    out = []
    for i in range(HOURS):
        k = start + i
        hour = (first_local_hour + i) % 24
        day_index = (first_local_hour + i) // 24
        label = "Maint." if i == 0 else WEEKDAYS[day_index] if hour == 0 else f"{hour}h"
        color = t["Text"] if i == 0 else t["Marker"] if hour == 0 else t["Muted"]
        x = MARGIN + i * cell
        uri = data_uri(ROOT / "res/icons" / f"{weather_icon(CODES[k], DAY[k])}.png")
        out.append(f'<div class="wh" style="left:{x:.1f}px;width:{cell:.1f}px;color:{color}">{label}</div>'
                   f'<div class="wi" style="left:{x + (cell - 30) / 2:.1f}px;background:{t["Text"]};'
                   f'-webkit-mask-image:url({uri})"></div>'
                   f'<div class="wt" style="left:{x:.1f}px;width:{cell:.1f}px;color:{t["Text"]}">{TEMPS[k]}°</div>')
    return "".join(out)


# Apps visibles relevées sur la TV (vd_applist), triées comme dans l'app.
INSTALLED = sorted(["Netflix", "Prime Video", "Disney+", "Apple TV", "Rakuten TV", "TF1+", "Orange TV", "Amazon Alexa",
                    "Browser", "Free TV", "france.tv", "CANAL+", "Pathé Home", "ARTE TV", "Spotify", "laCollection",
                    "Xbox", "GeForce NOW", "Amazon Luna", "e-Manual", "Hue Sync", "Plex", "Utomik", "Twitch", "2R2",
                    "M6+", "HBO Max", "Paramount+", "b.tv", "Jellyfin", "TizenTube"], key=str.lower)

FONTS = ('<link rel="preconnect" href="https://fonts.googleapis.com"><link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>'
         '<link href="https://fonts.googleapis.com/css2?family=DM+Sans:opsz,wght@9..40,200;9..40,300;9..40,400&display=swap" rel="stylesheet">')


def ambient_page(out):
    """Écran de veille, mêmes valeurs que HomeScreen.EnterAmbient (bloc à sa position de départ)."""
    wall = data_uri(ROOT / "res/wallpapers/kuri.jpg", "image/jpeg")
    page = f"""<!doctype html><html lang="fr"><head><meta charset="utf-8"><title>Quiet Home · veille</title>
{FONTS}
<style>
*{{margin:0;padding:0;box-sizing:border-box}}
html,body{{width:1920px;height:1080px;overflow:hidden;background:#0d0a08;font-family:"DM Sans",sans-serif}}
.wall{{position:absolute;inset:0;background:url({wall}) center/1920px 1080px;transform:scale(1.05) translateX(-18px)}}
.veil{{position:absolute;inset:0;background:linear-gradient(135deg,rgba(11,8,6,.58),rgba(5,4,3,.88))}}
.block{{position:absolute;left:{160 + 48}px;top:560px}}
.clock{{font-size:224px;font-weight:200;line-height:240px;color:rgba(236,227,212,.92);letter-spacing:-.02em}}
.line{{margin:22px 0 0 10px;width:72px;height:3px;border-radius:2px;background:#c79a5b}}
.date{{margin:25px 0 0 8px;font-size:35px;font-weight:300;color:rgba(236,227,212,.72)}}
.date b{{font-weight:300;color:#c79a5b}}
.brand{{position:absolute;right:160px;bottom:80px;font-size:15px;letter-spacing:.32em;color:rgba(236,227,212,.28)}}
</style></head><body>
<div class="wall"></div><div class="veil"></div>
<div class="block"><div class="clock">21:47</div><div class="line"></div>
<div class="date">mercredi 28 octobre &nbsp;·&nbsp; <b>Bonsoir</b></div></div>
<div class="brand">QUIET HOME</div>
</body></html>"""
    out.write_text(page, encoding="utf-8")
    print(out)


def grid_page(css_head, out):
    margin, top, gap, cw, ch = 80, 210, 20, 202, 130
    row_h = ch + 26 + 34
    body = ['<div class="gtitle">Toutes les apps</div>',
            f'<div class="gsub">{len(INSTALLED)} applications · maintenir OK pour les favoris</div>']
    for i, name in enumerate(INSTALLED[:32]):
        r, c = divmod(i, 8)
        body.append(card(margin + c * (cw + gap), top + 24 + r * row_h, cw, ch, name, focused=(i == 7)))
    out.write_text(css_head + "".join(body) + "</body></html>", encoding="utf-8")
    print(out)


def home_page(theme_id, clock, greeting, start, first_local_hour, focus_settings=False):
    t = theme(theme_id)
    soft = theme_id in ("kuri", "fume")
    rows_top, title_h, row_gap, gap = 340, 60, 40, 20
    cw, ch, kh = 202, 130, 104
    body, y = [], rows_top
    body.append(f'<div class="title" style="left:{MARGIN}px;top:{y}px">Applications</div>')
    for i, name in enumerate(["Netflix", "YouTube", "Jellyfin", "Spotify", "Prime Video", "Disney+", "Free TV", "Toutes les apps"]):
        body.append(card(MARGIN + i * (cw + gap), y + title_h, cw, ch, name, focused=(i == 3 and not focus_settings), soft=soft,
                         icon_name="apps" if name == "Toutes les apps" else None))
    y += title_h + ch + 26 + row_gap
    body.append(f'<div class="title" style="left:{MARGIN}px;top:{y}px">Consoles</div>')
    for i, (name, sub) in enumerate((("PS5", "HDMI 2"), ("Switch", "HDMI 4"))):
        body.append(card(MARGIN + i * (cw + gap), y + title_h, cw, kh, name, subtitle=sub, soft=soft))
    body.append(weather_strip(t, start, first_local_hour))

    settings = data_uri(ROOT / "res/icons/settings.png")
    theme_icon = data_uri(ROOT / "res/icons/theme.png")
    if soft:
        wall = data_uri(ROOT / f"res/wallpapers/{theme_id}.jpg", "image/jpeg")
        background = f"{t['BackgroundTo']} url({wall}) center/1920px 1080px"
    else:
        background = f"linear-gradient(to bottom right,{t['BackgroundFrom']},{t['BackgroundTo']})"
    page = f"""<!doctype html><html lang="fr"><head><meta charset="utf-8"><title>Quiet Home · {theme_id}</title>
{FONTS}
<style>
*{{margin:0;padding:0;box-sizing:border-box}}
html,body{{width:1920px;height:1080px;overflow:hidden}}
body{{position:relative;font-family:"DM Sans",sans-serif;color:{t['Text']};background:{background}}}
.clock{{position:absolute;left:{MARGIN - 8}px;top:64px;font-size:132px;font-weight:200;letter-spacing:-.02em;line-height:1}}
.date{{position:absolute;left:{MARGIN}px;top:220px;font-size:30px;font-weight:300;color:{t['Muted']}}}
.date b{{font-weight:300;color:{t['Marker']}}}
.btn{{position:absolute;top:96px;width:96px;height:96px;display:flex;align-items:center;justify-content:center}}
.btn span{{width:40px;height:40px;background:{t['Muted']};-webkit-mask:center/contain no-repeat}}
.btn.focus span{{background:{t['Text']};transform:scale(1.12)}}
.btn.focus::after{{content:"";position:absolute;left:34px;top:88px;width:28px;height:3px;border-radius:2px;background:{t['Marker']}}}
.title{{position:absolute;font-size:18px;font-weight:400;color:{t['Muted']};text-transform:uppercase;letter-spacing:.32em}}
.card{{position:absolute;border-radius:20px;overflow:hidden;
  box-shadow:inset 0 0 0 1px rgba(255,240,220,.09),0 18px 36px rgba(0,0,0,.45),0 4px 10px rgba(0,0,0,.35)}}
.card::after{{content:"";position:absolute;inset:0;background:linear-gradient(180deg,rgba(255,235,210,.07),transparent 45%)}}
.card.flat{{border-radius:22px;box-shadow:none}}
.card.flat::after{{display:none}}
.card.focus{{transform:translateY(-8px) scale(1.06);box-shadow:inset 0 0 0 1.5px {t['Marker']},0 26px 48px rgba(0,0,0,.55)}}
.card.flat.focus{{box-shadow:none}}
.label{{position:absolute;left:18px;right:18px;bottom:12px;font-size:21px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;font-weight:300;color:#f7f3ec}}
.sub{{position:absolute;right:16px;bottom:10px;font-size:16px;letter-spacing:.06em;color:#f7f3ec;opacity:.7}}
.marker{{position:absolute;width:56px;height:3px;border-radius:2px;background:{t['Marker']}}}
.wh,.wt{{position:absolute;text-align:center;white-space:nowrap}}
.wh{{top:{WEATHER_TOP + 3}px;font-size:{13 * PT:.0f}px;font-weight:400}}
.wi{{position:absolute;top:{WEATHER_TOP + 38}px;width:30px;height:30px;-webkit-mask:center/contain no-repeat}}
.wt{{top:{WEATHER_TOP + 76}px;font-size:{16 * PT:.0f}px;font-weight:300}}
</style></head><body>
<div class="clock">{clock}</div><div class="date">mercredi 28 octobre · <b>{greeting}</b></div>
<div class="btn" style="left:{1920 - MARGIN - 216}px"><span style="-webkit-mask-image:url({theme_icon})"></span></div>
<div class="btn{' focus' if focus_settings else ''}" style="left:{1920 - MARGIN - 96}px"><span style="-webkit-mask-image:url({settings})"></span></div>
{''.join(body)}
</body></html>"""
    return page


def main(out_dir=None):
    out_dir = pathlib.Path(out_dir or ROOT / "preview")
    out_dir.mkdir(parents=True, exist_ok=True)
    night = home_page("kuri", "21:47", "Bonsoir", start=20, first_local_hour=21)
    (out_dir / "accueil-kuri.html").write_text(night, encoding="utf-8")
    print(out_dir / "accueil-kuri.html")
    day = home_page("hinoki", "10:24", "Bonjour", start=9, first_local_hour=10)
    (out_dir / "accueil-hinoki.html").write_text(day, encoding="utf-8")
    print(out_dir / "accueil-hinoki.html")
    # Les autres thèmes : sombres le soir, clairs le jour, comme avec le thème automatique.
    for theme_id, dark in (("fume", True), ("sumi", True), ("japandi", False)):
        page = home_page(theme_id, "21:47", "Bonsoir", 20, 21) if dark else home_page(theme_id, "10:24", "Bonjour", 9, 10)
        (out_dir / f"accueil-{theme_id}.html").write_text(page, encoding="utf-8")
        print(out_dir / f"accueil-{theme_id}.html")
    (out_dir / "accueil-kuri-reglages.html").write_text(
        home_page("kuri", "21:47", "Bonsoir", start=20, first_local_hour=21, focus_settings=True), encoding="utf-8")
    head = night.split("<body>")[0].replace("</style>", """
.gtitle{position:absolute;left:110px;top:80px;font-size:72px;font-weight:200}
.gsub{position:absolute;left:110px;top:168px;font-size:22px;font-weight:300;color:#a8988a}
.veil{position:absolute;inset:0;background:rgba(18,12,8,.94)}
</style>""") + '<body><div class="veil"></div>'
    grid_page(head, out_dir / "toutes-les-apps-kuri.html")
    ambient_page(out_dir / "veille-kuri.html")


if __name__ == "__main__":
    main(*sys.argv[1:])
