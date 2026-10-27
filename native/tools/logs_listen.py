"""Reçoit les logs UDP de Quiet Home et les écrit (horodatés) dans un fichier.

    python -I logs_listen.py <fichier> [durée_minutes] [port]

Variante non interactive de `tv-native.ps1 -Action logs`, utile en arrière-plan.
"""
import socket
import sys
import time


def main(path, minutes="60", port="5140"):
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind(("0.0.0.0", int(port)))
    sock.settimeout(2)
    end = time.time() + float(minutes) * 60
    with open(path, "a", encoding="utf-8", buffering=1) as f:
        f.write(f"--- écoute UDP :{port} ---\n")
        while time.time() < end:
            try:
                data, addr = sock.recvfrom(65535)
            except socket.timeout:
                continue
            f.write(f"{time.strftime('%H:%M:%S')} {addr[0]} {data.decode('utf-8', 'replace')}\n")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    main(*sys.argv[1:])
