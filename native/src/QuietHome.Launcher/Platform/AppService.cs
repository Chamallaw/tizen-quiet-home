using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuietHome.Core;
using Tizen.Applications;

namespace QuietHome.Platform
{
    /// <summary>Liste et lancement des apps via les API publiques Tizen.Applications.</summary>
    public sealed class AppService
    {
        public async Task<List<AppEntry>> ListAsync()
        {
            var infos = await ApplicationManager.GetInstalledApplicationsAsync();
            var list = new List<AppEntry>();
            foreach (var info in infos)
            {
                try
                {
                    list.Add(new AppEntry(info.ApplicationId, info.Label, info.IconPath, info.IsNoDisplay));
                }
                catch (Exception e)
                {
                    Log.Warn("app ignorée : " + e.Message);
                }
            }
            Log.Info(list.Count + " apps installées (" + list.Count(a => !a.NoDisplay) + " visibles)");
            return list;
        }

        /// <summary>Identifiants des apps que le système déclare au premier plan.</summary>
        public async Task<List<string>> ForegroundAsync()
        {
            var running = await ApplicationManager.GetRunningApplicationsAsync();
            return running.Where(r => r.State == ApplicationRunningContext.AppState.Foreground)
                          .Select(r => r.ApplicationId).ToList();
        }

        /// <summary>Lance une app ; retourne false (et loggue) en cas d'échec, sans lever.</summary>
        public bool Launch(string appId, IDictionary<string, string> extras = null)
        {
            if (string.IsNullOrEmpty(appId)) return false;
            try
            {
                var ac = new AppControl { ApplicationId = appId, Operation = AppControlOperations.Default };
                if (extras != null)
                    foreach (var kv in extras) ac.ExtraData.Add(kv.Key, kv.Value);
                AppControl.SendLaunchRequest(ac);
                Log.Info("lancement " + appId);
                return true;
            }
            catch (Exception e)
            {
                Log.Error("lancement " + appId + " échoué", e);
                return false;
            }
        }
    }
}
