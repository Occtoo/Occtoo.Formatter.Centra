using Microsoft.Azure.Storage.Queue;
using outfit_international.Helpers;
using outfit_international.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace outfit_international.Services
{
    public enum OrchestratorState
    {
        Idle = 1,
        ProductsImporting = 2,
    }

    public static class OutfitOrchestratorService
    {
        private static async Task SetOrchestratorCurrentStateAsync(OrchestratorState currentState)
        {
            var azureVariable = new AzureVariable<OrchestratorOptions>();
            var options = await azureVariable.WaitOnLockAsync();
            options.IsProcessing = false;
            if (options.ImportIsForced)
            {
                options.ImportIsForced = false;
                options.ForcedImportRecordsCount = 0;
                options.CurrentState = OrchestratorState.ProductsImporting;
            }
            else
                options.CurrentState = currentState;
            await azureVariable.SaveAndUnlockAsync(options);
        }

        private static async Task<int> SetRecordsCountAsync(int recordsCount)
        {
            var azureVariable = new AzureVariable<OrchestratorOptions>();
            var options = await azureVariable.WaitOnLockAsync();
            options.RecordsCount = recordsCount;
            if (options.ImportIsForced && options.ForcedImportRecordsCount > 0 && options.ForcedImportRecordsCount < recordsCount)
                options.RecordsCount = options.ForcedImportRecordsCount;
            await azureVariable.SaveAndUnlockAsync(options);
            return options.RecordsCount;
        }

        public static async Task ImportProductsAsync(CloudQueue productMasterQueue)
        {
            var occtooSvc = new OcctooProductService(250) { };
            var centraSvc = new CentraService() { };
            var productMasters = new List<string>() { };
            var date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            await TableLogger.AddToLogAsync($"Started at: {DateTime.Now}");
            try
            {
                productMasters = await occtooSvc.GetDistinctProductMasterListAsync(date);

                var recCount = await SetRecordsCountAsync(productMasters.Count);
                await TableLogger.AddToLogAsync($"Total count: {productMasters.Count}, Processed count: {recCount}");

                await centraSvc.PrepareStoresAndMarket();
                var current = 0;
                foreach (var productMaster in productMasters)
                {
                    await productMasterQueue.AddMessageAsync(new CloudQueueMessage(productMaster));
                    current += 1;
                    if (current == recCount)
                        break;
                }
            }
            catch (Exception ex)
            {
                productMasters.Clear();
                await TableLogger.AddToLogAsync(ex.Message);
            }
            finally
            {
                if (productMasters.Count == 0)
                {
                    await SetOrchestratorCurrentStateAsync(OrchestratorState.Idle);
                    await TableLogger.AddToLogAsync($"There is no data for date: {date}");
                }
            }
        }
    }
}
