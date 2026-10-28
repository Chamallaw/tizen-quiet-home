using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tizen.Multimedia;

namespace QuietHome.Platform
{
    /// <summary>
    /// Son du déplacement du curseur (res/sounds/tick-&lt;nom&gt;.wav), joué sans bloquer l'interface.
    /// Un nouvel appui coupe le son précédent (touche maintenue). Ne lève jamais : en cas d'échec, il se tait.
    /// </summary>
    public sealed class UiSound
    {
        readonly string directory;
        AudioStreamPolicy policy;
        CancellationTokenSource playing;
        bool broken;
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
                playing?.Cancel();
                playing = new CancellationTokenSource();
                WavPlayer.StartAsync(path, policy, playing.Token).ContinueWith(t =>
                {
                    var e = t.Exception?.GetBaseException();
                    if (e != null && !(e is OperationCanceledException)) Log.Warn("son du curseur : " + e.Message);
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
