using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using outfit_international.Helpers;
using outfit_international.Model;
using outfit_international.Services;
using System;
using System.Threading.Tasks;

namespace outfit_international
{
    public class Orchestrator
    {
        [FunctionName(nameof(Orchestrator))]
        public async Task Run([TimerTrigger("0 */2 * * *", RunOnStartup = false, UseMonitor = false)] TimerInfo myTimer,
            [Queue("%ProductMasterQueue%"), StorageAccount("AzureWebJobsStorage")] CloudQueue productMasterQueue)
        {
            var (options, alreadyInProcessing) = await IsInOrchestratorAsync();
            if (options == null || alreadyInProcessing)
                return;

            try
            {
                await OutfitOrchestratorService.ImportProductsAsync(productMasterQueue);
            }
            catch (Exception ex)
            {
                await TableLogger.AddToLogAsync(ex.Message);
            }
        }

        private static async Task<(OrchestratorOptions, bool)> IsInOrchestratorAsync()
        {
            try
            {
                var azureVariable = new AzureVariable<OrchestratorOptions>();
                var options = await azureVariable.WaitOnLockAsync(TimeSpan.FromSeconds(30));
                var isBussy = options.IsProcessing;
                options.IsProcessing = true;
                await azureVariable.SaveAndUnlockAsync(options);
                return (options, isBussy);
            }
            catch
            {
                return (null, true);
            }
        }
    }
}