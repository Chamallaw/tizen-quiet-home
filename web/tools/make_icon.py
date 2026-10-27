"""Génère src/icon.png (512x512) sans dépendance : cercle bleu + carré blanc."""
import struct, zlib, pathlib, sys

SIZE = 512
out = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else pathlib.Path(__file__).parent.parent / "src" / "icon.png")

rows = []
c, r2 = SIZE / 2, (SIZE * 0.46) ** 2
for y in range(SIZE):
    row = bytearray([0])  # filtre PNG "None"
    for x in range(SIZE):
        inside = (x - c) ** 2 + (y - c) ** 2 <= r2
        house = abs(x - c) < 90 and abs(y - c - 20) < 80
        roof = 0 <= (c - 60) - y + 0 and abs(x - c) <= y - (c - 160) and y < c - 60
        if inside and (house or roof):
            row += bytes((255, 255, 255, 255))
        elif inside:
            row += bytes((79, 157, 255, 255))
        else:
            row += bytes((0, 0, 0, 0))
    rows.append(bytes(row))

def chunk(t, d):
    return struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xFFFFFFFF)

png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", SIZE, SIZE, 8, 6, 0, 0, 0))
png += chunk(b"IDAT", zlib.compress(b"".join(rows), 9)) + chunk(b"IEND", b"")
out.write_bytes(png)
print(f"écrit {out} ({len(png)} octets)")
