using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using outfit_international.Exceptions;
using outfit_international.Helpers;
using outfit_international.Model;
using outfit_international.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace outfit_international
{
    public class ProcessProductMasterFromQueue
    {
        [FunctionName("ProcessProductMasterFromQueue")]
        [Singleton(Mode = SingletonMode.Listener)]
        public async Task Run([QueueTrigger("%ProductMasterQueue%", Connection = "AzureWebJobsStorage")] CloudQueueMessage productMasterMessage,
                    [Queue("%ProductMasterQueue%"), StorageAccount("AzureWebJobsStorage")] CloudQueue productMasterQueue)
        {
            var tryAgain = false;
            var productMaster = productMasterMessage.AsString;
            var parts = productMaster.Split("_");
            var number = 1;
            productMaster = parts[0];
            if (parts.Length > 1 && int.TryParse(parts[1], out var intNumber))
                number = intNumber + 1;
            var azureVariable = new AzureVariable<OrchestratorOptions>();
            var options = (OrchestratorOptions)null;
            try
            {
                options = await azureVariable.WaitOnLockAsync(TimeSpan.FromMinutes(1));
                var state = options.CurrentState;
                if (number > 5)
                {
                    await TableLogger.AddToLogAsync($"[{productMaster} - after {number - 1} times skipping.]");
                    return;
                }

                var task = (Task)null;
                switch (state)
                {
                    case OrchestratorState.ProductsImporting:
                        task = ProcessProductFromQueueAsync(productMaster);
                        break;
                }
                if (task != null)
                    await task.WaitAsync(TimeSpan.FromSeconds(520));
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case OcctooServerError:
                    case CentraServerError:
                    case TimeoutException:
                        tryAgain = true;
                        break;
                    default:
                        await TableLogger.AddToLogAsync($"ERR[{productMaster}-{ex.Message}]");
                        break;
                }
            }
            finally
            {
                if (tryAgain)
                {
                    await azureVariable.UnlockAsync();
                    await productMasterQueue.AddMessageAsync(new CloudQueueMessage($"{productMaster}_{number}"));
                }
                else
                {
                    options.RecordsCount -= 1;
                    if (options.RecordsCount == 0)
                    {
                        options.IsProcessing = false;

                        if (options.ImportIsForced)
                        {
                            options.ImportIsForced = false;
                            options.CurrentState = OrchestratorState.ProductsImporting;
                        }
                        else
                        {
                            options.CurrentState = OrchestratorState.Idle;
                        }
                        await azureVariable.SaveAndUnlockAsync(options);

                        await TableLogger.AddToLogAsync($"Ended at: {DateTime.Now}");
                    }
                    else
                        await azureVariable.SaveAndUnlockAsync(options);
                }
            }
        }

        #region ProcessProductFromQueueAsync
        private static async Task ProcessProductFromQueueAsync(string productMaster)
        {
            var centraSvc = new CentraService() { };
            var processLog = new List<string>() { };
            try
            {
                processLog = await centraSvc.ImportProductFromProductMasterAsync(productMaster);
            }
            finally
            {
                if (processLog.Any())
                    await TableLogger.AddToLogAsync(processLog);
            }
        }
        #endregion
    }
}
