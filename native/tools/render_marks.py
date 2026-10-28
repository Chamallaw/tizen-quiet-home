"""Rastérise les logos SVG (res/marks-src) en PNG blancs, à leur taille d'affichage exacte.

    python -I render_marks.py <chrome.exe> [res_dir]

Chrome headless dessine chaque logo en blanc sur fond transparent (masque CSS). Un premier rendu
à 512 px donne le cadre visible du logo ; le logo est ensuite redessiné en vectoriel, directement
aux pixels de l'écran 1920x1080 de la TV, pour chaque gabarit de TileView :
    res/icons/<nom>.png        cartes et grille (boîte 0,56 × 202 sur 0,46 × 130)
    res/icons/<nom>-hdmi.png   tuiles d'appareils HDMI (boîte 0,50 × 202 sur 0,38 × 104)
Aucune réduction à l'affichage : les logos restent nets. La couleur est appliquée par l'app (teinte NUI).
"""
import base64
import pathlib
import struct
import subprocess
import sys
import tempfile
import zlib

SIZE = 512


def read_png_rgba(path):
    data = pathlib.Path(path).read_bytes()
    pos, idat, w, h, ctype = 8, b"", 0, 0, 0
    while pos < len(data):
        n = struct.unpack(">I", data[pos:pos + 4])[0]
        kind, body = data[pos + 4:pos + 8], data[pos + 8:pos + 8 + n]
        if kind == b"IHDR":
            w, h, depth, ctype = struct.unpack(">IIBB", body[:10])
            if depth != 8 or ctype not in (2, 6):
                sys.exit(f"PNG non géré : profondeur {depth}, type {ctype}")
        elif kind == b"IDAT":
            idat += body
        pos += 12 + n
    bpp = 4 if ctype == 6 else 3
    raw, stride = zlib.decompress(idat), w * bpp
    rows, prev = [], bytearray(stride)
    for y in range(h):
        f = raw[y * (stride + 1)]
        line = bytearray(raw[y * (stride + 1) + 1:(y + 1) * (stride + 1)])
        for i in range(stride):
            a = line[i - bpp] if i >= bpp else 0
            b = prev[i]
            c = prev[i - bpp] if i >= bpp else 0
            if f == 1: line[i] = (line[i] + a) & 255
            elif f == 2: line[i] = (line[i] + b) & 255
            elif f == 3: line[i] = (line[i] + (a + b) // 2) & 255
            elif f == 4:
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                line[i] = (line[i] + (a if pa <= pb and pa <= pc else b if pb <= pc else c)) & 255
        rows.append(line)
        prev = line
    if bpp == 3:
        sys.exit("capture sans canal alpha : fond transparent non pris en compte")
    return w, h, rows


def write_png(path, w, h, rows):
    def chunk(t, d):
        return struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xFFFFFFFF)
    raw = b"".join(b"\x00" + bytes(r) for r in rows)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    pathlib.Path(path).write_bytes(png)


def bounds(w, h, rows):
    """Cadre (x0, y0, x1, y1) des pixels visibles."""
    ys = [y for y in range(h) if any(rows[y][x * 4 + 3] for x in range(w))]
    xs = [x for x in range(w) if any(rows[y][x * 4 + 3] for y in ys)]
    return min(xs), min(ys), max(xs) + 1, max(ys) + 1


# Gabarits de TileView : suffixe du fichier, boîte maximale du logo en pixels.
BOXES = (("", 202 * 0.56, 130 * 0.46), ("-hdmi", 202 * 0.50, 104 * 0.38))


def shoot(chrome, tmp, name, uri, size, left, top, w, h):
    """Dessine le SVG en size × size, décalé de (left, top) ; l'appelant garde le coin w × h
    (fenêtre d'au moins 500 × 400 : Chrome headless bloque sur les très petites fenêtres)."""
    page = tmp / f"{name}.html"
    page.write_text(
        "<!doctype html><html><body style='margin:0;background:transparent;overflow:hidden'>"
        f"<div style='position:absolute;left:{left}px;top:{top}px;width:{size}px;height:{size}px;background:#fff;"
        f"-webkit-mask:url({uri}) center/contain no-repeat'></div></body></html>", encoding="utf-8")
    shot = tmp / f"{name}.png"
    subprocess.run([chrome, "--headless=new", "--disable-gpu", "--hide-scrollbars",
                    f"--user-data-dir={tmp / 'profile'}", "--default-background-color=00000000",
                    f"--window-size={max(w, 500)},{max(h, 400)}", f"--screenshot={shot}", page.as_uri()],
                   check=True, capture_output=True, timeout=60)
    return read_png_rgba(shot)


def main(chrome, res):
    res = pathlib.Path(res)
    src, out = res / "marks-src", res / "icons"
    out.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        tmp = pathlib.Path(tmp)
        for svg in sorted(src.glob("*.svg")):
            uri = "data:image/svg+xml;base64," + base64.b64encode(svg.read_bytes()).decode()
            x0, y0, x1, y1 = bounds(*shoot(chrome, tmp, svg.stem, uri, SIZE, 0, 0, SIZE, SIZE))
            for suffix, box_w, box_h in BOXES:
                k = min(box_w / (x1 - x0), box_h / (y1 - y0))
                w, h = max(1, round((x1 - x0) * k)), max(1, round((y1 - y0) * k))
                img_w, img_h, rows = shoot(chrome, tmp, svg.stem + suffix, uri, SIZE * k, -x0 * k, -y0 * k, w, h)
                rows = [r[:w * 4] for r in rows[:h]]
                write_png(out / f"{svg.stem}{suffix}.png", w, h, rows)
                print(f"{svg.stem}{suffix}.png  {w}x{h}")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else pathlib.Path(__file__).parent.parent / "res")
