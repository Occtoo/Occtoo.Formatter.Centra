using outfit_international.Dtos.Centra;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace outfit_international.Services
{
    public interface ICentraService
    {
        Task PrepareStoresAndMarket();
        Task<List<string>> ImportProductFromProductMasterAsync(string productMaster);

        Task<ProductGraphDto> GetProductGraphAsync(int productId);
        Task<int> GetProductIdAsync(string productNumber, string status = "ACTIVE");
    }
}
