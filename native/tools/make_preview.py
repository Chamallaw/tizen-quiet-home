"""Maquette HTML 1920x1080 de l'accueil, fidèle aux valeurs de HomeScreen/TileView/Themes.

    python -I make_preview.py [theme] [sortie.html]

Sert à valider le design sans être devant la TV. Approximations :
- PointSize NUI converti en px avec un facteur fixe (PT) ;
- logos officiels embarqués (res/icons) centrés et teints comme dans TileView ; autres apps : nom seul.
"""
import base64
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
PT = 1.33  # px par point, approximatif

themes_cs = (ROOT / "src/QuietHome.Core/Themes.cs").read_text(encoding="utf-8")


def theme(theme_id):
    block = re.search(r'Id = "%s".*?Overlay = "[^"]+"' % theme_id, themes_cs, re.S).group(0)
    return dict(re.findall(r'(\w+) = "(#[0-9a-fA-F]+)"', block), Name=re.search(r'Name = "([^"]+)"', block).group(1))


BRANDS = [(k, m, a, b, c or "#ffffff") for k, m, a, b, c in re.findall(
    r'new Brand\("([^"]+)", "([^"]+)", "(#\w+)", "(#\w+)"(?:, "(#\w+)")?\)', themes_cs)]


def brand(label):
    l = label.lower()
    return next((b for b in BRANDS if b[0] in l), None)


def tone(label):
    b = brand(label)
    return (b[2], b[3]) if b else ("#8a8178", "#4b453f")


def png_size(name):
    head = (ROOT / "res/icons" / f"{name}.png").read_bytes()[:24]
    return int.from_bytes(head[16:20], "big"), int.from_bytes(head[20:24], "big")


def icon(name):
    data = (ROOT / "res/icons" / f"{name}.png").read_bytes()
    return "data:image/png;base64," + base64.b64encode(data).decode()


def card(x, y, w, h, label, focused=False, icon_name=None, subtitle=None):
    a, b = tone(label)
    scale = "transform:scale(1.06);" if focused else ""
    parts = [f'<div class="card" style="left:{x}px;top:{y}px;width:{w}px;height:{h}px;'
             f'background:linear-gradient(135deg,{a},{b});{scale}">']
    b = brand(label)
    if b:
        iw, ih = png_size(b[1])
        k = min(w * 0.56 / iw, h * 0.46 / ih)
        bw, bh = iw * k, ih * k
        parts.append(f'<div style="position:absolute;left:{(w - bw) / 2}px;top:{(h - bh) / 2}px;width:{bw}px;height:{bh}px;'
                     f'background:{b[4]};-webkit-mask:url({icon(b[1])}) center/contain no-repeat"></div>')
    elif icon_name:
        parts.append(f'<img src="{icon(icon_name)}" style="left:24px;top:22px;width:56px;height:56px">')
    if subtitle:
        parts.append(f'<div class="sub" style="font-size:{12*PT:.0f}px">{subtitle}</div>')
    if not b:
        parts.append(f'<div class="label" style="font-size:{21*PT:.0f}px">{label}</div>')
    parts.append('</div>')
    if focused:
        parts.append(f'<div class="marker" style="left:{x + (w - 36) / 2}px;top:{y + h + 18}px"></div>')
    return "".join(parts)


def main(theme_id="japandi", out=None):
    t = theme(theme_id)
    margin, rows_top, title_h, row_gap, gap = 110, 330, 60, 40, 28
    cw, ch, kh = 260, 160, 130
    html = []
    y = rows_top
    html.append(f'<div class="title" style="left:{margin}px;top:{y}px">APPLICATIONS</div>')
    apps = ["Netflix", "Jellyfin", "Spotify", "Prime Video", "Disney+", "Toutes les apps"]
    for i, name in enumerate(apps):
        html.append(card(margin + i * (cw + gap), y + title_h, cw, ch, name, focused=(i == 0),
                         icon_name="apps" if name == "Toutes les apps" else None))
    y += title_h + ch + 26 + row_gap
    html.append(f'<div class="title" style="left:{margin}px;top:{y}px">CONSOLES</div>')
    for i, (name, sub) in enumerate((("PS5", "HDMI 2"), ("Switch", "HDMI 4"))):
        html.append(card(margin + i * (cw + gap), y + title_h, cw, kh, name, subtitle=sub))

    page = f"""<!doctype html><html lang="fr"><head><meta charset="utf-8"><title>Quiet Home</title>
<style>
*{{margin:0;padding:0;box-sizing:border-box}}
html,body{{width:1920px;height:1080px;overflow:hidden}}
body{{position:relative;font-family:"Segoe UI","Helvetica Neue",Arial,sans-serif;
  background:linear-gradient(135deg,{t['BackgroundFrom']},{t['BackgroundTo']});color:{t['Text']}}}
.clock{{position:absolute;left:{margin-6}px;top:70px;font-size:{84*PT:.0f}px;font-weight:200;line-height:160px}}
.date{{position:absolute;left:{margin}px;top:228px;font-size:{22*PT:.0f}px;color:{t['Muted']}}}
.btn{{position:absolute;left:{1920-margin-96}px;top:96px;width:96px;height:96px;border-radius:48px;background:{t['Surface']};
  display:flex;align-items:center;justify-content:center}}
.btn span{{width:44px;height:44px;background:{t['Text']};-webkit-mask:url({icon('settings')}) center/contain no-repeat;mask:url({icon('settings')}) center/contain no-repeat}}
.title{{position:absolute;font-size:{14*PT:.0f}px;color:{t['Muted']};letter-spacing:.08em}}
.card{{position:absolute;border-radius:22px;overflow:hidden;transform-origin:center}}
.card img{{position:absolute}}
.label{{position:absolute;left:24px;right:24px;bottom:18px;color:#f7f3ec;font-weight:300;line-height:1.1}}
.sub{{position:absolute;right:24px;top:24px;color:#f7f3ec;opacity:.7}}
.marker{{position:absolute;width:36px;height:4px;border-radius:2px;background:{t['Text']}}}
</style></head><body>
<div class="clock">14:32</div><div class="date">lundi 27 octobre</div>
<div class="btn"><span></span></div>
{''.join(html)}
</body></html>"""
    out = pathlib.Path(out or ROOT / "preview" / f"accueil-{theme_id}.html")
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(page, encoding="utf-8")
    print(out)


if __name__ == "__main__":
    main(*sys.argv[1:])
