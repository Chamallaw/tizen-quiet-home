using System;
using System.Collections.Generic;
using System.Linq;

namespace QuietHome.Core
{
    /// <summary>Type d'appareil branché sur une entrée HDMI : nom de la tuile, logo ou pictogramme, et s'il a une tuile.</summary>
    public sealed class DeviceKind
    {
        public readonly string Key, Label, Icon;
        /// <summary>Faux pour « Rien ».</summary>
        public readonly bool Tile;
        public readonly bool Console;

        public DeviceKind(string key, string label, string icon = null, bool tile = true, bool console = false)
        {
            Key = key; Label = label; Icon = icon; Tile = tile; Console = console;
        }
    }

    /// <summary>
    /// Ce qui est branché sur chaque entrée HDMI, réglé depuis l'app (Réglages → Entrées HDMI).
    /// Une entrée de LauncherConfig.Inputs par prise utilisée ; l'identifiant connect_source vaut 12 + n° de prise.
    /// </summary>
    public static class HdmiPorts
    {
        /// <summary>Prises HDMI proposées dans les réglages (4 sur la QN700B).</summary>
        public const int Count = 4;
        public const int FirstSourceId = 13;
        public const string None = "aucun";

        /// <summary>Ordre du réglage ◀ ▶ ; « aucun » d'abord.</summary>
        public static readonly IReadOnlyList<DeviceKind> Kinds = new List<DeviceKind>
        {
            new DeviceKind(None, "Rien", tile: false),
            new DeviceKind("ps5", "PS5", "playstation", console: true),
            new DeviceKind("switch", "Switch", "nintendoswitch", console: true),
            new DeviceKind("xbox", "Xbox", "xbox", console: true),
            new DeviceKind("decodeur", "Décodeur TV", "device-decodeur"),
            new DeviceKind("appletv", "Apple TV", "appletv"),
            new DeviceKind("shield", "Shield", "nvidia"),
            new DeviceKind("barre", "Barre de son", "device-barre"),
            new DeviceKind("pc", "PC", "device-pc"),
            new DeviceKind("bluray", "Blu-ray", "device-bluray"),
            new DeviceKind("autre", "Appareil", "device-autre"),
        };

        public static int SourceId(int port) => FirstSourceId + port - 1;
        public static int Port(InputSpec input) => input.SourceId - FirstSourceId + 1;

        public static DeviceKind Get(string key) => Kinds.FirstOrDefault(k => k.Key == key) ?? Kinds.First(k => k.Key == "autre");

        /// <summary>Type d'une entrée : celui enregistré, sinon déduit du nom (configurations d'avant ce réglage).</summary>
        public static DeviceKind KindOf(InputSpec input)
        {
            if (!string.IsNullOrEmpty(input.Kind) && Kinds.Any(k => k.Key == input.Kind)) return Get(input.Kind);
            var l = ((input.Label ?? "") + " " + (input.Icon ?? "")).ToLowerInvariant();
            if (l.Contains("ps5") || l.Contains("playstation")) return Get("ps5");
            if (l.Contains("switch") || l.Contains("nintendo")) return Get("switch");
            if (l.Contains("xbox")) return Get("xbox");
            return Get("autre");
        }

        /// <summary>Appareil branché sur la prise n (1 à Count), « Rien » si aucun.</summary>
        public static DeviceKind At(LauncherConfig cfg, int port)
        {
            var input = cfg.Inputs.FirstOrDefault(i => i.Enabled && Port(i) == port);
            return input == null ? Get(None) : KindOf(input);
        }

        /// <summary>Type suivant (ou précédent) pour le réglage ◀ ▶.</summary>
        public static string Next(string key, int step)
        {
            int i = Math.Max(0, Kinds.ToList().FindIndex(k => k.Key == key));
            return Kinds[((i + step) % Kinds.Count + Kinds.Count) % Kinds.Count].Key;
        }

        /// <summary>Copie de la configuration avec cet appareil sur la prise n ; les entrées restent triées par prise.</summary>
        public static LauncherConfig Set(LauncherConfig cfg, int port, string key)
        {
            var next = ConfigStore.Clone(cfg);
            next.Inputs.RemoveAll(i => Port(i) == port);
            if (key != None)
            {
                var kind = Get(key);
                next.Inputs.Add(new InputSpec(kind.Label, SourceId(port), kind.Icon) { Kind = kind.Key });
            }
            next.Inputs.Sort((a, b) => a.SourceId.CompareTo(b.SourceId));
            return next;
        }

        /// <summary>Titre de la rangée : « Consoles » s'il n'y a que des consoles, sinon « Appareils ».</summary>
        public static string RowTitle(IEnumerable<InputSpec> shown) =>
            shown.All(i => KindOf(i).Console) ? "Consoles" : "Appareils";
    }
}
