"""Génère les icônes du launcher (PNG RGBA) sans dépendance, par champs de distance signée.

    python -I make_icons.py [dossier_res]

Produit :
    res/quiethome.png        icône de l'app (512) : ensō sur galet de sable, sceau argile
    res/icons/apps.png       grille 3x3 (blanc)
    res/icons/settings.png   curseurs de réglage (blanc, teinté par l'UI)
    res/icons/theme.png      cercle à moitié plein : changer de thème
    res/icons/device-*.png   appareils HDMI sans logo : décodeur, barre de son, PC, Blu-ray, autre
    res/icons/weather-*.png  météo au trait : soleil, lune, nuages, brouillard, bruine, pluie, neige, orage
Les icônes blanches sont teintées ou posées sur les dégradés par l'interface.
Ce sont des pictogrammes génériques dessinés ici ; les logos officiels viennent de fetch_marks.py.
"""
import math
import pathlib
import struct
import sys
import zlib


# ---------- primitives SDF (distance < 0 : intérieur) ----------

def sd_circle(x, y, cx, cy, r):
    return math.hypot(x - cx, y - cy) - r


def sd_ring(x, y, cx, cy, r, w):
    return abs(math.hypot(x - cx, y - cy) - r) - w / 2


def sd_round_rect(x, y, cx, cy, hw, hh, r):
    qx, qy = abs(x - cx) - hw + r, abs(y - cy) - hh + r
    return math.hypot(max(qx, 0), max(qy, 0)) + min(max(qx, qy), 0) - r


def sd_segment(x, y, ax, ay, bx, by, w):
    px, py, vx, vy = x - ax, y - ay, bx - ax, by - ay
    h = max(0.0, min(1.0, (px * vx + py * vy) / (vx * vx + vy * vy)))
    return math.hypot(px - vx * h, py - vy * h) - w / 2


def sd_polyline(x, y, pts, w, closed=True):
    n = len(pts) if closed else len(pts) - 1
    return min(sd_segment(x, y, *pts[i], *pts[(i + 1) % len(pts)], w) for i in range(n))


def outline(d, w):
    """Transforme une forme pleine en contour d'épaisseur w."""
    return abs(d) - w / 2


def cov(d):
    """Couverture anti-crénelée d'un pixel pour une distance en pixels."""
    return max(0.0, min(1.0, 0.5 - d))


# ---------- rendu ----------

def render(size, shade):
    """shade(x, y) -> (r, g, b, a) en 0..255 (a peut être float)."""
    rows = []
    for j in range(size):
        row = bytearray([0])
        for i in range(size):
            r, g, b, a = shade(i + 0.5, j + 0.5)
            row += bytes((int(r), int(g), int(b), int(max(0, min(255, a)))))
        rows.append(bytes(row))
    return rows


def write_png(path, size, rows):
    def chunk(t, d):
        return struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xFFFFFFFF)
    data = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0))
    data += chunk(b"IDAT", zlib.compress(b"".join(rows), 9)) + chunk(b"IEND", b"")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)
    print(f"{path} ({len(data)} octets)")


def white(sdf, size=None):
    """Pictogramme blanc. Avec size, dessiné directement à cette taille en pixels (dessin conçu sur S = 256) :
    l'anticrénelage tombe sur les vrais pixels de la TV, sans réduction floue à l'affichage."""
    k = S / size if size else 1.0
    return lambda x, y: (255, 255, 255, 255 * cov(sdf(x * k, y * k) / k))


def hexrgb(h):
    return tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))


def mix(a, b, t):
    return tuple(a[k] + (b[k] - a[k]) * t for k in range(3))


def over(dst, src, alpha):
    """Composite src (rgb) avec alpha sur dst (rgb, a)."""
    r, g, b, a = dst
    out_a = alpha + a * (1 - alpha)
    if out_a <= 0:
        return (0, 0, 0, 0)
    col = [(src[k] * alpha + (r, g, b)[k] * a * (1 - alpha)) / out_a for k in range(3)]
    return (*col, out_a)


# ---------- icônes ----------

S = 256


def ps5(x, y):
    w = 13
    q = S / 4
    tri = sd_polyline(x, y, [(q, q - 36), (q - 38, q + 30), (q + 38, q + 30)], w)
    circ = sd_ring(x, y, 3 * q, q, 36, w)
    cross = min(sd_segment(x, y, q - 32, 3 * q - 32, q + 32, 3 * q + 32, w),
                sd_segment(x, y, q - 32, 3 * q + 32, q + 32, 3 * q - 32, w))
    sq = outline(sd_round_rect(x, y, 3 * q, 3 * q, 33, 33, 3), w)
    return min(tri, circ, cross, sq)


def switch(x, y):
    w = 12
    left = outline(sd_round_rect(x, y, 80, 128, 38, 96, 38), w)
    right = outline(sd_round_rect(x, y, 176, 128, 38, 96, 38), w)
    stick = sd_circle(x, y, 80, 92, 15)
    buttons = min(sd_circle(x, y, 176, 158, 15), sd_circle(x, y, 176, 92, 6))
    return min(left, right, stick, buttons)


def apps(x, y):
    return min(sd_round_rect(x, y, 64 + 64 * i, 64 + 64 * j, 20, 20, 7) for i in range(3) for j in range(3))


def settings(x, y):
    """Trois curseurs au trait : ligne interrompue autour d'un bouton annulaire (même trait que la météo)."""
    d = 1e9
    for ly, kx in ((72, 160), (128, 96), (184, 176)):
        line = max(sd_segment(x, y, 36, ly, 220, ly, WS), -sd_circle(x, y, kx, ly, 32))
        d = min(d, line, sd_ring(x, y, kx, ly, 20, WS))
    return d


def theme(x, y):
    """Cercle au trait, moitié gauche pleine (jour / nuit), séparée du contour par un jour."""
    c = S / 2
    ring = sd_ring(x, y, c, c, 84, WS)
    half = max(sd_circle(x, y, c, c, 62), x - c)
    return min(ring, half)


# ---------- météo : pictogrammes au trait, lisibles à 30 px ----------

WS = 16  # épaisseur du trait


def smin(a, b, k):
    """Union douce de deux formes (k : rayon du raccord)."""
    h = max(k - abs(a - b), 0.0) / k
    return min(a, b) - h * h * k / 4


def cloud_fill(x, y, ox=0.0, oy=0.0, s=1.0):
    """Nuage plein : base arrondie et deux bosses fondues. ox, oy : décalage ; s : échelle."""
    x, y = (x - 128 - ox) / s + 128, (y - 128 - oy) / s + 128
    d = smin(sd_round_rect(x, y, 128, 166, 86, 36, 36), sd_circle(x, y, 98, 138, 42), 18)
    return smin(d, sd_circle(x, y, 150, 120, 56), 18) * s


def sun_shape(x, y, cx, cy, r):
    """Soleil : cercle et huit rayons."""
    d = sd_ring(x, y, cx, cy, r, WS)
    for k in range(8):
        c, s = math.cos(k * math.pi / 4), math.sin(k * math.pi / 4)
        d = min(d, sd_segment(x, y, cx + c * r * 1.5, cy + s * r * 1.5, cx + c * r * 1.95, cy + s * r * 1.95, WS))
    return d


def moon_shape(x, y, cx, cy, r):
    """Croissant."""
    crescent = max(sd_circle(x, y, cx, cy, r), -sd_circle(x, y, cx + r * 0.45, cy - r * 0.40, r * 0.80))
    return outline(crescent, WS)


def behind_cloud(shape, ox, oy, s):
    """Astre en haut à gauche, masqué par un nuage au premier plan (avec un léger jour)."""
    def sdf(x, y):
        fill = cloud_fill(x, y, ox, oy, s)
        return min(max(shape(x, y), -(fill - 12)), outline(fill, WS))
    return sdf


def with_precipitation(marks):
    """Nuage remonté et, dessous, des gouttes, des flocons ou un éclair."""
    def sdf(x, y):
        return min(outline(cloud_fill(x, y, 0, -30, 0.92), WS), marks(x, y))
    return sdf


def weather_icons():
    rain = lambda x, y: min(sd_segment(x, y, cx + 7, top, cx - 7, top + 34, WS)
                            for cx, top in ((92, 188), (128, 200), (164, 188)))
    drizzle = lambda x, y: min(sd_segment(x, y, cx + 3, top, cx - 3, top + 12, WS)
                               for cx, top in ((92, 190), (128, 204), (164, 190), (110, 224), (146, 224)))
    snow = lambda x, y: min(sd_circle(x, y, cx, cy, 9)
                            for cx, cy in ((92, 194), (128, 208), (164, 194), (110, 230), (146, 230)))
    bolt = lambda x, y: sd_polyline(x, y, [(142, 176), (116, 210), (146, 210), (120, 244)], WS, closed=False)
    fog = lambda x, y: min(sd_segment(x, y, 60, 198, 180, 198, WS), sd_segment(x, y, 92, 228, 204, 228, WS))
    return {
        "weather-sun": lambda x, y: sun_shape(x, y, 128, 128, 40),
        "weather-moon": lambda x, y: moon_shape(x, y, 120, 136, 76),
        "weather-sun-cloud": behind_cloud(lambda x, y: sun_shape(x, y, 92, 92, 28), 22, 30, 0.80),
        "weather-moon-cloud": behind_cloud(lambda x, y: moon_shape(x, y, 92, 96, 50), 22, 30, 0.80),
        "weather-cloud": lambda x, y: outline(cloud_fill(x, y, 0, -4), WS),
        "weather-fog": with_precipitation(fog),
        "weather-drizzle": with_precipitation(drizzle),
        "weather-rain": with_precipitation(rain),
        "weather-snow": with_precipitation(snow),
        "weather-storm": with_precipitation(bolt),
    }


# ---------- appareils HDMI sans logo de marque : même trait que la météo ----------

def device_icons():
    def decoder(x, y):
        box = outline(sd_round_rect(x, y, 128, 132, 92, 36, 14), WS)
        return min(box, sd_circle(x, y, 186, 132, 9), sd_segment(x, y, 64, 132, 120, 132, WS),
                   sd_segment(x, y, 64, 186, 84, 186, WS), sd_segment(x, y, 172, 186, 192, 186, WS))

    def soundbar(x, y):
        bar = outline(sd_round_rect(x, y, 128, 128, 104, 26, 24), WS)
        return min(bar, *(sd_circle(x, y, cx, 128, 8) for cx in (72, 128, 184)))

    def pc(x, y):
        screen = outline(sd_round_rect(x, y, 128, 108, 90, 62, 12), WS)
        return min(screen, sd_segment(x, y, 128, 178, 128, 206, WS), sd_segment(x, y, 84, 210, 172, 210, WS))

    def disc(x, y):
        return min(sd_ring(x, y, 128, 128, 86, WS), sd_ring(x, y, 128, 128, 22, WS))

    def plug(x, y):
        # Prise HDMI vue de face : trapèze aux coins arrondis et ses contacts.
        shell = sd_polyline(x, y, [(48, 92), (208, 92), (208, 136), (180, 168), (76, 168), (48, 136)], WS)
        return min(shell, sd_segment(x, y, 84, 124, 172, 124, WS))

    return {"device-decodeur": decoder, "device-barre": soundbar, "device-pc": pc,
            "device-bluray": disc, "device-autre": plug}


def quiethome(size=512):
    sand, clay = hexrgb("#efe7dc"), hexrgb("#d3c1aa")
    ink, seal = hexrgb("#2b2723"), hexrgb("#b0503c")
    c = size / 2

    def shade(x, y):
        tile = sd_round_rect(x, y, c, c, c - 8, c - 8, 112)
        a = cov(tile)
        if a <= 0:
            return (0, 0, 0, 0)
        t = ((x + y) / (2 * size))
        px = (*mix(sand, clay, t), a)
        # Ensō : anneau au pinceau, épaisseur variable, ouvert en haut à droite.
        ang = math.atan2(y - (c + 6), x - (c - 4))
        gap = -1.05 < ang < -0.55
        thick = 30 + 14 * math.sin(ang * 1.0 + 0.6)
        ring = sd_ring(x, y, c - 4, c + 6, 150, thick)
        if not gap:
            px = over(px, ink, cov(ring) * a)
        # Extrémités arrondies de l'ouverture.
        for ea in (-1.05, -0.55):
            ex, ey = c - 4 + 150 * math.cos(ea), c + 6 + 150 * math.sin(ea)
            tw = 30 + 14 * math.sin(ea + 0.6)
            px = over(px, ink, cov(sd_circle(x, y, ex, ey, tw / 2)) * a)
        # Sceau (hanko).
        px = over(px, seal, cov(sd_round_rect(x, y, c + 172, c + 172, 24, 24, 6)) * a)
        r, g, b, al = px
        return (r, g, b, al * 255)

    return render(size, shade)


def main(res):
    res = pathlib.Path(res)
    # Chaque pictogramme est produit à sa taille d'affichage exacte (px de l'écran 1920x1080 de la TV) :
    # boutons ronds 40, tuile « Toutes les apps » et appareils 36, météo 32 (trait de 2 px pile).
    sizes = {"apps": 36, "settings": 40, "theme": 40}
    icons = [("apps", apps), ("settings", settings), ("theme", theme)]
    icons += [(n, f) for n, f in weather_icons().items()] + [(n, f) for n, f in device_icons().items()]
    for name, fn in icons:
        size = sizes.get(name, 32 if name.startswith("weather-") else 36)
        write_png(res / "icons" / f"{name}.png", size, render(size, white(fn, size)))
    write_png(res / "quiethome.png", 512, quiethome())


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else pathlib.Path(__file__).parent.parent / "res")
