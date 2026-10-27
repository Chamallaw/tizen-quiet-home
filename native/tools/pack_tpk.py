"""Assemble un .tpk non signé (zip) à partir de la sortie de build.

Usage : python -I pack_tpk.py <bin_dir> <manifest> <res_dir> <version> <out.tpk>

Arborescence produite (format TPK .NET) :
    tizen-manifest.xml
    bin/QuietHome.Launcher.dll, bin/QuietHome.Core.dll
    res/icons/*.png                (logos et pictogrammes des tuiles)
    res/wallpapers/*.jpg           (fonds 3840x2160, générés par make_wallpaper.py)
    res/fonts/*.ttf + OFL.txt      (DM Sans, fetch_fonts.py)
    shared/res/quiethome.png       (icône de l'app)
Les assemblies Tizen et le runtime ne sont pas inclus : la TV les fournit.
La signature est ajoutée ensuite par `tizen package -t tpk -s <profil>`.
"""
import pathlib
import sys
import zipfile

# Seules nos assemblies : deps.json/runtimeconfig ne servent pas au lanceur dotnet de Tizen.
PAYLOAD = ("QuietHome.Launcher.dll", "QuietHome.Core.dll")


def main(bin_dir, manifest, res_dir, version, out):
    bin_dir, res_dir, out = pathlib.Path(bin_dir), pathlib.Path(res_dir), pathlib.Path(out)
    icons = sorted((res_dir / "icons").glob("*.png"))
    if not icons or not (res_dir / "quiethome.png").exists():
        sys.exit(f"icônes absentes de {res_dir} : lancez tools/make_icons.py")
    xml = pathlib.Path(manifest).read_text(encoding="utf-8").replace("{VERSION}", version)
    if "{VERSION}" in xml or f'version="{version}"' not in xml:
        sys.exit("manifeste : substitution de version échouée")
    missing = [n for n in PAYLOAD if not (bin_dir / n).exists()]
    if missing:
        sys.exit(f"build incomplet, manquant : {missing}")

    out.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
        z.writestr("tizen-manifest.xml", xml)
        for name in PAYLOAD:
            z.write(bin_dir / name, f"bin/{name}")
        for icon in icons:
            z.write(icon, f"res/icons/{icon.name}")
        for wall in sorted((res_dir / "wallpapers").glob("*.jpg")):
            z.write(wall, f"res/wallpapers/{wall.name}")
        for font in sorted((res_dir / "fonts").glob("*")):
            z.write(font, f"res/fonts/{font.name}")
        z.write(res_dir / "quiethome.png", "shared/res/quiethome.png")
    print(f"{out} ({out.stat().st_size} octets)")
    for info in zipfile.ZipFile(out).infolist():
        print(f"  {info.filename}")


if __name__ == "__main__":
    if len(sys.argv) != 6:
        sys.exit(__doc__)
    main(*sys.argv[1:])
