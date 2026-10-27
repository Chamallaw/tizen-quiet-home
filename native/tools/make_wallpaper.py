"""Génère les fonds « mur de tasseaux » en 3840x2160 (JPEG), voile de lisibilité intégré.

    python -I make_wallpaper.py <chrome.exe> [nom ...]

Chaque lame a son propre veinage (feTurbulence, graine différente) et une teinte légèrement
variable, comme du vrai bois ; des joints d'ombre en V les séparent. Le rendu se fait en
1920x1080 logiques avec un facteur 2 : le bruit est calculé en pleine résolution 4K, donc net
sur une grande dalle. Une couche de fibres fines donne le grain du bois, et un bruit léger
évite le banding des dégradés sombres.
Conversion PNG → JPEG (qualité 92) via System.Drawing (PowerShell Windows).
Sur la TV, le fond n'est qu'une image statique, décodée à la taille de la fenêtre.
"""
import pathlib
import random
import subprocess
import sys
import tempfile

W, H, SLAT = 1920, 1080, 80

# nom : (couleur de base RGB 0..1, variation de teinte, contraste du veinage, voile haut, voile bas)
WOODS = {
    "kuri": ((0.30, 0.19, 0.12), 0.035, 0.30, "rgba(16,11,8,.42)", "rgba(9,6,4,.74)"),   # noyer
    "fume": ((0.25, 0.22, 0.19), 0.025, 0.26, "rgba(14,12,10,.44)", "rgba(8,7,6,.76)"),   # chêne fumé
}


def svg(name):
    (r, g, b), var, contrast, _, _ = WOODS[name]
    rnd = random.Random(name)
    defs, rects = [], []
    for i, x in enumerate(range(0, W, SLAT)):
        k = 1 + rnd.uniform(-var, var) * 4          # luminosité propre à la lame
        warm = rnd.uniform(-var, var) * 0.35        # léger glissement de teinte, sans virer au rouge
        cr, cg, cb = r * k + warm, g * k, b * k - warm / 2
        fx = rnd.uniform(0.045, 0.07)
        fy = rnd.uniform(0.0012, 0.0022)
        seed = rnd.randint(1, 9999)
        c = contrast
        defs.append(f"""<filter id="s{i}" x="0" y="0" width="100%" height="100%" color-interpolation-filters="sRGB">
  <feTurbulence type="fractalNoise" baseFrequency="{fx:.4f} {fy:.5f}" numOctaves="6" seed="{seed}"/>
  <feColorMatrix type="matrix" values="{c * cr:.3f} 0 0 0 {cr * (1 - c / 2):.3f}  {c * cg:.3f} 0 0 0 {cg * (1 - c / 2):.3f}  {c * cb:.3f} 0 0 0 {cb * (1 - c / 2):.3f}  0 0 0 0 1"/>
</filter>""")
        rects.append(f'<rect x="{x}" y="0" width="{SLAT}" height="{H}" filter="url(#s{i})"/>')
    return defs, rects


PAGE = """<!doctype html><html><body style="margin:0;width:{W}px;height:{H}px;overflow:hidden;background:#000">
<svg width="{W}" height="{H}" style="position:absolute;inset:0"><defs>{defs}
<filter id="fibres" x="0" y="0" width="100%" height="100%">
  <feTurbulence type="turbulence" baseFrequency="0.42 0.0045" numOctaves="3" seed="21"/>
  <feColorMatrix type="matrix" values="0 0 0 0 0.05  0 0 0 0 0.03  0 0 0 0 0.02  0.55 0 0 0 -0.08"/>
</filter>
<filter id="dither" x="0" y="0" width="100%" height="100%">
  <feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="1" seed="3"/>
  <feColorMatrix type="matrix" values="0 0 0 0 1  0 0 0 0 1  0 0 0 0 1  0 0 0 0.035 0"/>
</filter></defs>
{rects}
<rect width="{W}" height="{H}" filter="url(#fibres)"/>
<rect width="{W}" height="{H}" filter="url(#dither)"/>
</svg>
<!-- Joints en V : ombre franche à gauche de chaque lame, arête éclairée à droite. -->
<div style="position:absolute;inset:0;background:repeating-linear-gradient(90deg,
  rgba(0,0,0,.62) 0 2px,rgba(0,0,0,.30) 2px 4px,rgba(0,0,0,.10) 4px 7px,transparent 7px {s1}px,
  rgba(255,228,196,.045) {s1}px {SLAT}px)"></div>
<div style="position:absolute;inset:0;background:linear-gradient(180deg,{top},{bottom})"></div>
<div style="position:absolute;inset:0;background:
  radial-gradient(ellipse 110% 80% at 28% 18%,rgba(255,205,150,.07),transparent 60%),
  radial-gradient(ellipse 140% 120% at 50% 45%,transparent 55%,rgba(0,0,0,.42))"></div>
</body></html>"""

TO_JPEG = r"""
Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile($args[0])
$codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
$p = New-Object System.Drawing.Imaging.EncoderParameters(1)
$p.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter([System.Drawing.Imaging.Encoder]::Quality, [long]92)
$img.Save($args[1], $codec, $p)
"Size: " + $img.Width + "x" + $img.Height
$img.Dispose()
"""


def main(chrome, *names):
    out_dir = pathlib.Path(__file__).parent.parent / "res" / "wallpapers"
    out_dir.mkdir(parents=True, exist_ok=True)
    for name in names or WOODS:
        defs, rects = svg(name)
        _, _, _, top, bottom = WOODS[name]
        with tempfile.TemporaryDirectory() as tmp:
            tmp = pathlib.Path(tmp)
            page = tmp / "wood.html"
            page.write_text(PAGE.format(W=W, H=H, SLAT=SLAT, s1=SLAT - 3, defs="".join(defs), rects="".join(rects),
                                        top=top, bottom=bottom), encoding="utf-8")
            png = tmp / "wood.png"
            subprocess.run([chrome, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=2",
                            f"--user-data-dir={tmp / 'profile'}", f"--window-size={W},{H}", f"--screenshot={png}",
                            page.as_uri()], check=True, capture_output=True, timeout=300)
            script = tmp / "tojpeg.ps1"
            script.write_text(TO_JPEG, encoding="utf-8")
            jpg = out_dir / f"{name}.jpg"
            res = subprocess.run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(script), str(png), str(jpg)],
                                 check=True, capture_output=True, text=True, timeout=120)
            print(f"{jpg}  {res.stdout.strip()}  {jpg.stat().st_size // 1024} Ko")
    for old in out_dir.glob("*.png"):
        old.unlink()


if __name__ == "__main__":
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    main(*sys.argv[1:])
