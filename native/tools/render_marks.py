"""Rastérise les logos SVG (res/marks-src) en PNG blancs recadrés (res/icons/<nom>.png).

    python -I render_marks.py <chrome.exe> [res_dir]

Chrome headless dessine chaque logo en blanc sur fond transparent (masque CSS),
puis l'image est recadrée sur ses pixels visibles : le launcher lit la taille du PNG
pour respecter le rapport largeur/hauteur. La couleur est appliquée à l'exécution (teinte NUI).
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


def trim(w, h, rows, pad=4):
    ys = [y for y in range(h) if any(rows[y][x * 4 + 3] for x in range(w))]
    xs = [x for x in range(w) if any(rows[y][x * 4 + 3] for y in ys)]
    x0, x1 = max(0, min(xs) - pad), min(w, max(xs) + 1 + pad)
    y0, y1 = max(0, min(ys) - pad), min(h, max(ys) + 1 + pad)
    return x1 - x0, y1 - y0, [r[x0 * 4:x1 * 4] for r in rows[y0:y1]]


def main(chrome, res):
    res = pathlib.Path(res)
    src, out = res / "marks-src", res / "icons"
    out.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        tmp = pathlib.Path(tmp)
        for svg in sorted(src.glob("*.svg")):
            uri = "data:image/svg+xml;base64," + base64.b64encode(svg.read_bytes()).decode()
            page = tmp / f"{svg.stem}.html"
            page.write_text(
                "<!doctype html><html><body style='margin:0;background:transparent'>"
                f"<div style='width:{SIZE}px;height:{SIZE}px;background:#fff;"
                f"-webkit-mask:url({uri}) center/contain no-repeat'></div></body></html>",
                encoding="utf-8")
            shot = tmp / f"{svg.stem}.png"
            subprocess.run([chrome, "--headless=new", "--disable-gpu", "--hide-scrollbars",
                            f"--user-data-dir={tmp / 'profile'}", "--default-background-color=00000000",
                            f"--window-size={SIZE},{SIZE}", f"--screenshot={shot}", page.as_uri()],
                           check=True, capture_output=True, timeout=60)
            w, h, rows = trim(*read_png_rgba(shot))
            write_png(out / f"{svg.stem}.png", w, h, rows)
            print(f"{svg.stem}.png  {w}x{h}")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else pathlib.Path(__file__).parent.parent / "res")
