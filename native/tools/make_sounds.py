"""Synthétise les sons du curseur (WAV 16 bits mono, 48 kHz), sans dépendance.

    python -I make_sounds.py [dossier_res]

Principes (sons d'interface répétés des centaines de fois) : très courts (90 à 150 ms), sinusoïdes pures
sans bruit, attaque arrondie de quelques millisecondes, légère descente de hauteur, extinction exponentielle,
niveau bas (environ -16 dBFS) car le son suit le volume de la TV.

Produit res/sounds/ :
    tick-goutte.wav    goutte d'eau feutrée : une note ronde qui glisse à peine vers le bas
    tick-bois.wav      bois doux : note grave et son octave, comme un petit bloc de bois au maillet de feutre
    tick-kalimba.wav   kalimba : note claire avec la résonance métallique très légère d'une lame
"""
import math
import pathlib
import struct
import sys
import wave

RATE = 48000
LEVEL = 0.16   # crête, environ -16 dBFS


def voice(duration, partials, attack=0.004, glide=0.0):
    """
    Somme de partiels (fréquence, amplitude, constante de décroissance en s), avec une attaque en cosinus,
    une descente de hauteur relative « glide » sur les 40 premières ms et un fondu final de 20 ms.
    """
    n = int(RATE * duration)
    phases = [0.0] * len(partials)
    out = []
    for i in range(n):
        t = i / RATE
        bend = 1.0 - glide * min(1.0, t / 0.04)
        env_attack = 0.5 - 0.5 * math.cos(math.pi * min(1.0, t / attack))
        tail = 1.0 if t < duration - 0.02 else max(0.0, (duration - t) / 0.02)
        s = 0.0
        for k, (freq, amp, tau) in enumerate(partials):
            phases[k] += 2 * math.pi * freq * bend / RATE
            s += amp * math.sin(phases[k]) * math.exp(-t / tau)
        out.append(s * env_attack * tail)
    peak = max(abs(v) for v in out)
    return [v / peak * LEVEL for v in out]


SOUNDS = {
    "goutte": lambda: voice(0.11, [(698, 1.0, 0.030), (1396, 0.10, 0.012)], attack=0.003, glide=0.035),
    "bois": lambda: voice(0.13, [(330, 1.0, 0.040), (660, 0.30, 0.020), (990, 0.06, 0.010)], attack=0.005, glide=0.015),
    "kalimba": lambda: voice(0.15, [(587, 1.0, 0.050), (587 * 5.4, 0.05, 0.008)], attack=0.003, glide=0.005),
}


def write_wav(path, samples):
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(round(s * 32767))) for s in samples))
    print(f"{path} ({path.stat().st_size} octets)")


def main(res):
    sounds = pathlib.Path(res) / "sounds"
    for old in sounds.glob("*.wav"):
        old.unlink()
    for name, make in SOUNDS.items():
        write_wav(sounds / f"tick-{name}.wav", make())


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else pathlib.Path(__file__).parent.parent / "res")
