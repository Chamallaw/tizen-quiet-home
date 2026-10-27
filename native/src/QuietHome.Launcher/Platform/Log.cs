using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace QuietHome.Platform
{
    /// <summary>
    /// Journal : dlog (souvent inaccessible sur TV grand public), tampon mémoire visible
    /// dans Récupération > Diagnostic, et UDP optionnel vers le PC de dev (tools/logs.ps1).
    /// </summary>
    public static class Log
    {
        const string Tag = "QuietHome";
        const int Capacity = 200;
        static readonly LinkedList<string> buffer = new LinkedList<string>();
        static readonly object gate = new object();
        static UdpClient udp;
        static string udpHost;
        static int udpPort;

        public static void EnableUdp(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host)) return;
            try
            {
                udp = new UdpClient();
                udpHost = host;
                udpPort = port;
                Info("logs UDP vers " + host + ":" + port);
            }
            catch (Exception e)
            {
                udp = null;
                Warn("UDP indisponible : " + e.Message);
            }
        }

        public static void Info(string msg) => Write("I", msg);
        public static void Warn(string msg) => Write("W", msg);
        public static void Error(string msg, Exception e = null) => Write("E", e == null ? msg : msg + " : " + e.GetType().Name + " " + e.Message);

        public static IList<string> Recent()
        {
            lock (gate) return new List<string>(buffer);
        }

        static void Write(string level, string msg)
        {
            var line = DateTime.Now.ToString("HH:mm:ss.fff") + " " + level + " " + msg;
            lock (gate)
            {
                buffer.AddLast(line);
                while (buffer.Count > Capacity) buffer.RemoveFirst();
            }
            try
            {
                if (level == "E") Tizen.Log.Error(Tag, msg);
                else if (level == "W") Tizen.Log.Warn(Tag, msg);
                else Tizen.Log.Info(Tag, msg);
            }
            catch (Exception) { /* hors TV */ }
            try
            {
                if (udp != null)
                {
                    var bytes = Encoding.UTF8.GetBytes("[QuietHome] " + line);
                    udp.Send(bytes, bytes.Length, udpHost, udpPort);
                }
            }
            catch (Exception) { /* le log ne doit jamais faire planter l'accueil */ }
        }
    }
}
