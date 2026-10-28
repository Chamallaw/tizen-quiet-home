using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tizen.Multimedia;

namespace QuietHome.Platform
{
    /// <summary>
    /// Son du déplacement du curseur (res/sounds/tick-&lt;nom&gt;.wav), joué sans bloquer l'interface.
    /// Ne lève jamais : si le lecteur ne peut pas être créé, il se tait ; un échec de lecture isolé est journalisé une fois.
    /// </summary>
    public sealed class UiSound
    {
        readonly string directory;
        AudioStreamPolicy policy;
        bool broken, reported;
        string missing;

        public UiSound(string directory) { this.directory = directory; }

        public void Tick(string name)
        {
            if (broken || string.IsNullOrEmpty(name)) return;
            var path = Path.Combine(directory, "tick-" + name + ".wav");
            try
            {
                if (!File.Exists(path))
                {
                    if (missing != path) Log.Warn("son du curseur absent : " + path);
                    missing = path;
                    return;
                }
                if (policy == null) policy = new AudioStreamPolicy(AudioStreamType.Media);
                // Pas d'annulation du son précédent : sur la TV, WavPlayer échoue (« Failed to cancel »).
                // Les sons durent moins de 0,15 s, ils peuvent se chevaucher.
                WavPlayer.StartAsync(path, policy, CancellationToken.None).ContinueWith(t =>
                {
                    var e = t.Exception?.GetBaseException();
                    if (e != null && !reported) { reported = true; Log.Warn("son du curseur : " + e.Message); }
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            catch (Exception e)
            {
                broken = true;
                Log.Warn("son du curseur indisponible : " + e.GetType().Name + " " + e.Message);
            }
        }
    }
}
