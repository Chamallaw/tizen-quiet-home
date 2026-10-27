using System;
using System.Runtime.InteropServices;

namespace QuietHome.Platform
{
    /// <summary>
    /// Volume et sources via les bibliothèques natives Samsung (repérées dans quiet-tizen).
    /// Non garanties pour une app signée développeur : chaque appel échoue proprement et
    /// renvoie un message affichable au lieu de lever.
    /// </summary>
    public static class TvControls
    {
        const string SoundLib = "libcapi-media-sound-manager-tv.so.0";
        const string SourceLib = "libsource-api.so";

        [DllImport(SoundLib)] static extern int sound_manager_get_master_volume(out int level);
        [DllImport(SoundLib)] static extern int sound_manager_set_master_volume(int level);
        [DllImport(SoundLib)] static extern int sound_manager_get_master_mute(out int muted);
        [DllImport(SoundLib)] static extern int sound_manager_set_master_mute(int muted);
        [DllImport(SourceLib)] static extern int connect_source(int sourceType);

        public static string ChangeVolume(int delta)
        {
            return Guard("volume", () =>
            {
                int r = sound_manager_get_master_volume(out int level);
                if (r != 0) return "Volume illisible (" + r + ")";
                int next = Math.Max(0, Math.Min(100, level + delta));
                r = sound_manager_set_master_volume(next);
                return r == 0 ? "Volume " + next : "Volume refusé (" + r + ")";
            });
        }

        public static string ToggleMute()
        {
            return Guard("mute", () =>
            {
                int r = sound_manager_get_master_mute(out int muted);
                if (r != 0) return "Muet illisible (" + r + ")";
                r = sound_manager_set_master_mute(muted == 1 ? 0 : 1);
                return r != 0 ? "Muet refusé (" + r + ")" : muted == 1 ? "Son rétabli" : "Muet";
            });
        }

        /// <summary>Bascule la source ; l'appelant affiche ensuite le lecteur TV.</summary>
        public static bool TryConnectSource(int sourceId, out string message)
        {
            string msg = null;
            bool ok = false;
            msg = Guard("source " + sourceId, () =>
            {
                int r = connect_source(sourceId);
                ok = r == 0;
                return ok ? "Source " + sourceId : "Source " + sourceId + " refusée (" + r + ")";
            });
            message = msg;
            return ok;
        }

        static string Guard(string what, Func<string> action)
        {
            try
            {
                var msg = action();
                Log.Info(what + " : " + msg);
                return msg;
            }
            catch (DllNotFoundException)
            {
                Log.Warn(what + " : bibliothèque absente");
                return "Non disponible sur cette TV";
            }
            catch (EntryPointNotFoundException)
            {
                Log.Warn(what + " : fonction absente");
                return "Non disponible sur cette TV";
            }
            catch (Exception e)
            {
                Log.Error(what, e);
                return "Erreur : " + e.Message;
            }
        }
    }
}
