"""Télécharge DM Sans (SIL OFL) en TTF statiques, graisses 200/300/400/500, dans res/fonts/.

    python -I fetch_fonts.py

L'API CSS de Google Fonts sert des instances statiques .ttf aux clients qui ne gèrent pas
le WOFF2 : on lui présente un agent utilisateur minimal, puis on télécharge les URL listées.
La TV enregistre ce dossier via FontClient.AddCustomFontDirectory.
"""
import pathlib
import re
import sys
import urllib.request

CSS = "https://fonts.googleapis.com/css2?family=DM+Sans:wght@200;300;400;500"
LICENSE = "https://raw.githubusercontent.com/google/fonts/main/ofl/dmsans/OFL.txt"
UA = {"User-Agent": "Mozilla/4.0"}
NAMES = {"200": "ExtraLight", "300": "Light", "400": "Regular", "500": "Medium"}


def get(url, headers=None):
    with urllib.request.urlopen(urllib.request.Request(url, headers=headers or {}), timeout=30) as r:
        return r.read()


def main(out):
    out = pathlib.Path(out)
    out.mkdir(parents=True, exist_ok=True)
    css = get(CSS, UA).decode()
    faces = re.findall(r"font-weight:\s*(\d+);.*?src:\s*url\((https://[^)]+\.ttf)\)", css, re.S)
    if len(faces) < 4:
        sys.exit("réponse inattendue de Google Fonts :\n" + css[:500])
    for weight, url in faces:
        data = get(url)
        if data[:4] not in (b"\x00\x01\x00\x00", b"true"):
            sys.exit(f"{url} n'est pas un TTF")
        name = f"DMSans-{NAMES[weight]}.ttf"
        (out / name).write_bytes(data)
        print(f"{name}  {len(data) // 1024} Ko")
    (out / "OFL.txt").write_bytes(get(LICENSE))
    print("OFL.txt")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else pathlib.Path(__file__).parent.parent / "res" / "fonts")
