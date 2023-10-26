using outfit_international.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace outfit_international.Helpers
{
    public static class TableLogger
    {
        private static async Task<List<string>> GetLogAsync()
        {
            var azureVariable = new AzureVariable<OrchestratorLog>();
            var log = await azureVariable.LoadAsync();
            return log?.Log ?? new List<string>();
        }

        private static async Task ClearLogAsync()
        {
            var azureVariable = new AzureVariable<OrchestratorLog>();
            var log = await azureVariable.WaitOnLockAsync();
            log?.Log.Clear();
            await azureVariable.SaveAndUnlockAsync(log);
        }

        public static async Task AddToLogAsync(string logLine)
        {
            var azureVariable = new AzureVariable<OrchestratorLog>();
            var log = await azureVariable.WaitOnLockAsync();
            log?.Log.Add(logLine);
            await azureVariable.SaveAndUnlockAsync(log);
        }

        public static async Task AddToLogAsync(IEnumerable<string> logRange)
        {
            var azureVariable = new AzureVariable<OrchestratorLog>();
            var log = await azureVariable.WaitOnLockAsync();
            log?.Log.AddRange(logRange);
            await azureVariable.SaveAndUnlockAsync(log);
        }
    }
}
