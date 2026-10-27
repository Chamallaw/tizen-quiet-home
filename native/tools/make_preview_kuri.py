"""Maquette de test de l'ambiance « Kuri » (Japandi sombre et boisé), avant intégration dans l'app.

    python -I make_preview_kuri.py [sortie.html]

Changements par rapport à make_preview.py :
- fond : mur de tasseaux en noyer (res/wallpapers/kuri.jpg, 3840x2160, make_wallpaper.py) ;
- palette chaude : lin, brun charbon, accent ocre ;
- tuiles aux couleurs des marques, désaturées et assombries pour se fondre dans le bois,
  avec liseré clair et ombre portée ;
- police DM Sans (OFL), graisse légère ; salutation selon l'heure.
"""
import base64
import colorsys
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
themes_cs = (ROOT / "src/QuietHome.Core/Themes.cs").read_text(encoding="utf-8")

KURI = {
    "text": "#ece3d4",      # lin
    "muted": "#a8988a",     # lin grisé
    "accent": "#c79a5b",    # ocre
    "surface": "rgba(236,227,212,.08)",
}
SAT, LUM = 0.80, 0.82      # adoucissement des couleurs de marque

BRANDS = [(k, m, a, b, c or "#ffffff") for k, m, a, b, c in re.findall(
    r'new Brand\("([^"]+)", "([^"]+)", "(#\w+)", "(#\w+)"(?:, "(#\w+)")?\)', themes_cs)]


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


def card(x, y, w, h, label, focused=False, subtitle=None, icon_name=None):
    b = brand(label)
    a, z = (soften(b[2]), soften(b[3])) if b else ("#5b4a3c", "#33281f")
    cls = "card focus" if focused else "card"
    out = [f'<div class="{cls}" style="left:{x}px;top:{y}px;width:{w}px;height:{h}px;'
           f'background:linear-gradient(135deg,{a},{z})">']
    # Comme TileView : sans fichier de logo, la marque garde son dégradé mais affiche son nom.
    if b and (ROOT / "res/icons" / f"{b[1]}.png").exists():
        iw, ih = png_size(b[1])
        k = min(w * 0.56 / iw, h * (0.40 if subtitle else 0.46) / ih)
        bw, bh = iw * k, ih * k
        cy = h * (0.42 if subtitle else 0.5)
        mark = soften(b[4], 0.9, 0.95) if b[4].lower() != "#ffffff" else "#f3ece2"
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


# Apps visibles relevées sur la TV (vd_applist), triées comme dans l'app.
INSTALLED = sorted(["Netflix", "Prime Video", "Disney+", "Apple TV", "Rakuten TV", "TF1+", "Orange TV", "Amazon Alexa",
                    "Browser", "Free TV", "france.tv", "CANAL+", "Pathé Home", "ARTE TV", "Spotify", "laCollection",
                    "Xbox", "GeForce NOW", "Amazon Luna", "e-Manual", "Hue Sync", "Plex", "Utomik", "Twitch", "2R2",
                    "M6+", "HBO Max", "Paramount+", "b.tv", "Jellyfin", "TizenTube"], key=str.lower)


def ambient_page(out):
    """Écran de veille, mêmes valeurs que HomeScreen.EnterAmbient (bloc à sa position de départ)."""
    wall = data_uri(ROOT / "res/wallpapers/kuri.jpg", "image/jpeg")
    page = f"""<!doctype html><html lang="fr"><head><meta charset="utf-8"><title>Quiet Home · veille</title>
<link href="https://fonts.googleapis.com/css2?family=DM+Sans:opsz,wght@9..40,200;9..40,300;9..40,400&display=swap" rel="stylesheet">
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
<div class="date">lundi 27 octobre &nbsp;·&nbsp; <b>Bonsoir</b></div></div>
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


def main(out=None):
    margin, rows_top, title_h, row_gap, gap = 80, 340, 60, 40, 20
    cw, ch, kh = 202, 130, 104
    body, y = [], rows_top
    body.append(f'<div class="title" style="left:{margin}px;top:{y}px">Applications</div>')
    for i, name in enumerate(["Netflix", "YouTube", "Jellyfin", "Spotify", "Prime Video", "Disney+", "Free TV", "Toutes les apps"]):
        body.append(card(margin + i * (cw + gap), y + title_h, cw, ch, name, focused=(i == 0),
                         icon_name="apps" if name == "Toutes les apps" else None))
    y += title_h + ch + 26 + row_gap
    body.append(f'<div class="title" style="left:{margin}px;top:{y}px">Consoles</div>')
    for i, (name, sub) in enumerate((("PS5", "HDMI 2"), ("Switch", "HDMI 4"))):
        body.append(card(margin + i * (cw + gap), y + title_h, cw, kh, name, subtitle=sub))

    wall = data_uri(ROOT / "res/wallpapers/kuri.jpg", "image/jpeg")
    settings = data_uri(ROOT / "res/icons/settings.png")
    theme_icon = data_uri(ROOT / "res/icons/theme.png")
    t = KURI
    page = f"""<!doctype html><html lang="fr"><head><meta charset="utf-8"><title>Quiet Home · Kuri</title>
<link rel="preconnect" href="https://fonts.googleapis.com"><link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=DM+Sans:opsz,wght@9..40,200;9..40,300;9..40,400&display=swap" rel="stylesheet">
<style>
*{{margin:0;padding:0;box-sizing:border-box}}
html,body{{width:1920px;height:1080px;overflow:hidden}}
body{{position:relative;font-family:"DM Sans",sans-serif;color:{t['text']};background:#1a130e url({wall}) center/1920px 1080px}}
.clock{{position:absolute;left:{margin - 8}px;top:64px;font-size:132px;font-weight:200;letter-spacing:-.02em;line-height:1}}
.date{{position:absolute;left:{margin}px;top:220px;font-size:30px;font-weight:300;color:{t['muted']}}}
.date b{{font-weight:300;color:{t['accent']}}}
.btn{{position:absolute;top:96px;width:96px;height:96px;border-radius:48px;
  background:{t['surface']};box-shadow:inset 0 0 0 1px rgba(236,227,212,.14);display:flex;align-items:center;justify-content:center}}
.btn span{{width:42px;height:42px;background:{t['text']};-webkit-mask:center/contain no-repeat}}
.title{{position:absolute;font-size:18px;font-weight:400;color:{t['muted']};text-transform:uppercase;letter-spacing:.32em}}
.card{{position:absolute;border-radius:20px;overflow:hidden;
  box-shadow:inset 0 0 0 1px rgba(255,240,220,.09),0 18px 36px rgba(0,0,0,.45),0 4px 10px rgba(0,0,0,.35)}}
.card::after{{content:"";position:absolute;inset:0;background:linear-gradient(180deg,rgba(255,235,210,.07),transparent 45%)}}
.card.focus{{transform:translateY(-8px) scale(1.05);box-shadow:inset 0 0 0 1px rgba(199,154,91,.55),0 26px 48px rgba(0,0,0,.55)}}
.label{{position:absolute;left:18px;right:18px;bottom:12px;font-size:21px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;font-weight:300;color:#efe6d8}}
.sub{{position:absolute;right:16px;bottom:10px;font-size:15px;letter-spacing:.12em;color:#efe6d8;opacity:.65}}
.marker{{position:absolute;width:56px;height:3px;border-radius:2px;background:{t['accent']}}}
</style></head><body>
<div class="clock">21:47</div><div class="date">lundi 27 octobre · <b>Bonsoir</b></div>
<div class="btn" style="left:{1920 - margin - 216}px"><span style="-webkit-mask-image:url({theme_icon})"></span></div>
<div class="btn" style="left:{1920 - margin - 96}px"><span style="-webkit-mask-image:url({settings})"></span></div>
{''.join(body)}
</body></html>"""
    out = pathlib.Path(out or ROOT / "preview" / "accueil-kuri.html")
    out.write_text(page, encoding="utf-8")
    print(out)
    head = page.split("<body>")[0].replace("</style>", """
.gtitle{position:absolute;left:110px;top:80px;font-size:72px;font-weight:200}
.gsub{position:absolute;left:110px;top:168px;font-size:22px;font-weight:300;color:#a8988a}
.veil{position:absolute;inset:0;background:rgba(18,12,8,.94)}
</style>""") + '<body><div class="veil"></div>'
    grid_page(head, out.with_name("toutes-les-apps-kuri.html"))
    ambient_page(out.with_name("veille-kuri.html"))


if __name__ == "__main__":
    main(*sys.argv[1:])
