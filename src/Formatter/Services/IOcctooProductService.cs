using outfit_international.Dtos;
using outfit_international.Dtos.Occtoo;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace outfit_international.Services
{
    public interface IOcctooProductService
    {
        Task<List<string>> GetDistinctProductMasterListAsync(string newerThanDate = ""); //date format yyyy-mm-dd
        Task<List<ProductDto>> GetProductHierarchyAsync(string productMaster);
        Task<ProductHierarchyDto> CreateProductHierarchyAsync(List<ProductDto> products, int collectionId);
    }
}
