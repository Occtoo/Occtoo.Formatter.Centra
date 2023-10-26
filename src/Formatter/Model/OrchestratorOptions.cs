using outfit_international.Services;

namespace outfit_international.Model
{
    public class OrchestratorOptions
    {
        public bool ImportIsForced { get; set; } = false;
        public int ForcedImportRecordsCount { get; set; } = 0;
        public int RecordsCount { get; set; } = 0;
        public bool IsProcessing { get; set; } = false;
        public OrchestratorState CurrentState { get; set; } = OrchestratorState.ProductsImporting;

        public OrchestratorOptions()
        {
            IsProcessing = false;
            CurrentState = OrchestratorState.ProductsImporting;
        }
    }
}
