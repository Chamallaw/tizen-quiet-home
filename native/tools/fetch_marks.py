"""Télécharge les logos officiels (SVG monochromes) depuis des sources épinglées.

    python -I fetch_marks.py [dossier_sortie]

Mêmes sources et versions que NelsonEU/arn0-tv :
- Simple Icons v13.21.0 (CC0) via jsDelivr ;
- Custom Brand Icons, commit épinglé (MIT), pour Disney+.
Les SVG sont écrits tels quels dans res/marks-src/ avec un SOURCES.md.
Les noms et logos appartiennent à leurs propriétaires : usage personnel sur la TV de test.
"""
import pathlib
import sys
import urllib.error
import urllib.request

SIMPLE = "https://cdn.jsdelivr.net/npm/simple-icons@13.21.0/icons/"
CUSTOM = "https://raw.githubusercontent.com/elax46/custom-brand-icons/a53cabf49bf439023a8f33585af30d783bffe51b/icon-svg/"

MARKS = {
    # nom local : (URL, licence)
    "netflix": (SIMPLE + "netflix.svg", "Simple Icons, CC0"),
    "youtube": (SIMPLE + "youtube.svg", "Simple Icons, CC0"),
    "jellyfin": (SIMPLE + "jellyfin.svg", "Simple Icons, CC0"),
    "spotify": (SIMPLE + "spotify.svg", "Simple Icons, CC0"),
    "primevideo": (SIMPLE + "primevideo.svg", "Simple Icons, CC0"),
    "disneyplus": (CUSTOM + "disney-plus.svg", "Custom Brand Icons, MIT"),
    "playstation": (SIMPLE + "playstation.svg", "Simple Icons, CC0"),
    "nintendoswitch": (SIMPLE + "nintendoswitch.svg", "Simple Icons, CC0"),
}

# Autres apps installées sur la TV : ignorées si absentes de la source.
OPTIONAL = {
    "plex": (SIMPLE + "plex.svg", "Simple Icons, CC0"),
    "twitch": (SIMPLE + "twitch.svg", "Simple Icons, CC0"),
    "appletv": (SIMPLE + "appletv.svg", "Simple Icons, CC0"),
    "nvidia": (SIMPLE + "nvidia.svg", "Simple Icons, CC0"),
    "canalplus": (SIMPLE + "canalplus.svg", "Simple Icons, CC0"),
    "arte": (SIMPLE + "arte.svg", "Simple Icons, CC0"),
    "francetelevisions": (SIMPLE + "francetelevisions.svg", "Simple Icons, CC0"),
    "xbox": (CUSTOM + "xbox.svg", "Custom Brand Icons, MIT"),
}


def main(out):
    out = pathlib.Path(out)
    out.mkdir(parents=True, exist_ok=True)
    lines = ["# Sources des logos", "", "| Fichier | Source | Licence |", "|---|---|---|"]
    for name, (url, licence) in list(MARKS.items()) + list(OPTIONAL.items()):
        try:
            with urllib.request.urlopen(url, timeout=20) as r:
                data = r.read()
        except urllib.error.HTTPError as e:
            if name in OPTIONAL:
                print(f"{name} : absent ({e.code}), ignoré")
                continue
            raise
        if not data.lstrip().startswith(b"<svg"):
            sys.exit(f"{name} : la réponse n'est pas un SVG ({url})")
        (out / f"{name}.svg").write_bytes(data)
        lines.append(f"| {name}.svg | {url} | {licence} |")
        print(f"{name}.svg  {len(data)} octets")
    lines += ["", "Les noms et logos de marques appartiennent à leurs propriétaires."]
    (out / "SOURCES.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else pathlib.Path(__file__).parent.parent / "res" / "marks-src")
