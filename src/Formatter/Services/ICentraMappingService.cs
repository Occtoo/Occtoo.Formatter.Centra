using System.Collections.Generic;

namespace outfit_international.Services
{
    public interface ICentraMappingService
    {
        int GetCentraSizeChartId(string productType);
        int GetCentraFolderId(string productType);
        int GetCentraMeasurementChartId(string productType);
        (int, List<int>) GetCentraCanonicalandCategoryIds(string productType);
    }
}
