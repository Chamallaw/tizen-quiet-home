"""Génère les icônes du launcher (PNG RGBA) sans dépendance, par champs de distance signée.

    python -I make_icons.py [dossier_res]

Produit :
    res/quiethome.png        icône de l'app (512) : ensō sur galet de sable, sceau argile
    res/icons/apps.png       grille 3x3 (blanc)
    res/icons/settings.png   curseurs de réglage (blanc, teinté par l'UI)
    res/icons/theme.png      cercle à moitié plein : changer de thème
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


def white(sdf):
    return lambda x, y: (255, 255, 255, 255 * cov(sdf(x, y)))


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
    d = 1e9
    for k, (ly, kx) in enumerate(((72, 160), (128, 96), (184, 176))):
        d = min(d, sd_segment(x, y, 40, ly, 216, ly, 12))
        d = min(d, sd_circle(x, y, kx, ly, 22))
    return d


def theme(x, y):
    c = S / 2
    ring = sd_ring(x, y, c, c, 82, 14)
    half = max(sd_circle(x, y, c, c, 76), x - c)   # moitié gauche pleine
    return min(ring, half)


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
    for name, fn in (("apps", apps), ("settings", settings), ("theme", theme)):
        write_png(res / "icons" / f"{name}.png", S, render(S, white(fn)))
    write_png(res / "quiethome.png", 512, quiethome())


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else pathlib.Path(__file__).parent.parent / "res")
