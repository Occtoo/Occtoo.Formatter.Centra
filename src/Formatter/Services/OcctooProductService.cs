using outfit_international.Dtos;
using outfit_international.Dtos.Centra;
using outfit_international.Dtos.Occtoo;
using outfit_international.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace outfit_international.Services
{
    public class OcctooProductService : OcctooDestinationService, IOcctooProductService
    {
        private readonly int _entitiesPerPage = 50;
        private static readonly string _endpointTestProducts = "availableproducts";

        public OcctooProductService(int entitiesPerPage = 50) : base(
            Settings.OcctooUrl,
            Settings.OcctooUrlToken,
            Settings.OcctooClientId,
            Settings.OcctooClientSecret) =>
                _entitiesPerPage = entitiesPerPage;

        public async Task<List<string>> GetDistinctProductMasterListAsync(string newerThanDate = "")
        {
            var result = new List<string>() { };

            var periodSince = "";
            if (newerThanDate != "")
                periodSince = $"&periodSince={newerThanDate}T00:00:00";

            var page = await GetObjectsAsync<ProductDto>(_endpointTestProducts, $"top={_entitiesPerPage}&sortAsc=id{periodSince}");
            foreach (var line in page)
                if (!result.Contains(line.productMaster))
                    result.Add(line.productMaster);
            while (page.Count() == _entitiesPerPage)
            {
                var queryStr = $"top={_entitiesPerPage}&sortAsc=id{periodSince}&after={page.Last().id}";
                page = await GetObjectsAsync<ProductDto>(_endpointTestProducts, queryStr);
                foreach (var line in page)
                    if (!result.Contains(line.productMaster))
                        result.Add(line.productMaster);
            }

            return result;
        }

        public async Task<List<ProductDto>> GetProductHierarchyAsync(string productMaster)
        {
            var page = await GetObjectsAsync<ProductDto>(_endpointTestProducts, $"language=en&top={_entitiesPerPage}&productMaster={productMaster}&sortAsc=id");
            var products = new List<ProductDto>(page);
            while (page.Count() == _entitiesPerPage)
            {
                var queryStr = $"language=en&top={_entitiesPerPage}&productMaster={productMaster}&sortAsc=id&after={page.Last().id}";

                page = await GetObjectsAsync<ProductDto>(_endpointTestProducts, queryStr);
                if (page.Any())
                    products.AddRange(page);
            }

            return products;
        }

        public async Task<ProductHierarchyDto> CreateProductHierarchyAsync(List<ProductDto> products, int collectionId)
        {
            string JoinArrayWithPipes(string[] array)
            {
                return (array == null || array.Length == 0) ? "" : string.Join("|", array);
            }

            string GetStatusByLifecycle(string lifecycle)
            {
                var result = "INACTIVE";
                if (int.TryParse(lifecycle, out int lifecycleCode))
                    result = (lifecycleCode >= 21 || lifecycleCode <= 27) ? "ACTIVE" : "INACTIVE";

                return result;
            }

            string GetProductName()
            {
                try
                {
                    var result = products.GroupBy(x => x.productName)
                        .Select(y => y.First());

                    var prodName = result.Where(x => !string.IsNullOrEmpty(x.productName))
                        .Select(y => y.productName)
                        .FirstOrDefault();

                    if (!string.IsNullOrEmpty(prodName))
                    {
                        while (prodName.IndexOf('\"') > 0)
                            prodName = prodName.Remove(prodName.IndexOf('\"'), 1);
                    }

                    return prodName == null ? "" : prodName;
                }
                catch
                {
                    return string.Empty;
                }
            }

            string GetItemWeight()
            {
                foreach (var product in products)
                    if (float.TryParse(product.itemWeight, out var weight) && (weight > 0))
                        return product.itemWeight;

                return products[0].itemWeight;
            }

            if (products.Count == 0)
                throw new Exception("Product list is empty.");

            foreach (var prod in products)
            {
                while (prod.productVar1Value.Length < 2)
                    prod.productVar1Value = "0" + prod.productVar1Value;
                while (prod.itemVar2Value.Length < 2)
                    prod.itemVar2Value = "0" + prod.itemVar2Value;
            }

            var brandName = products[0].productBCBrand ?? "";
            if (string.IsNullOrEmpty(brandName))
                throw new Exception("Product brand is not set.");

            var productName = GetProductName();
            if (productName == "")
                throw new Exception("Product name is not set.");

            var centraMapper = new CentraMappingService();
            var centraSvc = new CentraService();
            var folderId = centraMapper.GetCentraFolderId(products[0].productTypeID);

            var excludedFromMarkets = (List<string>)null;
            if (products[0].productExcludedMarkets != null)
                excludedFromMarkets = new List<string>(products[0].productExcludedMarkets);
            else
                excludedFromMarkets = new List<string>();

            var product = new ProductHierarchyDto()
            {
                BrandId = 1, // need to create atleast one brand in Centra and put the id here.
                CentraId = await centraSvc.GetProductIdAsync(products[0].productMaster),
                CollectionId = collectionId,
                FolderId = centraMapper.GetCentraFolderId(products[0].productTypeID),
                MeasurementChartId = centraMapper.GetCentraMeasurementChartId(products[0].productTypeID),
                Name = productName,
                Description = products[0].productShortWebText,
                ProductNumber = products[0].productMaster,
                Status = GetStatusByLifecycle(products[0].productLifecycle),
                InternalComment = "",
                Weight = GetItemWeight(),
                ExcludedFromMarkets = excludedFromMarkets
            };

            var variants = products.GroupBy(x => x.productVar1Value)
                .Select(g => g.First())
                .ToList();

            var productGraph = await centraSvc.GetProductGraphAsync(product.CentraId);

            if (productGraph == null)
                productGraph = new ProductGraphDto();

            product.Variants = new List<ProductVariantDto>() { };
            foreach (var variant in variants)
            {
                var sizeChartId = centraMapper.GetCentraSizeChartId(variant.productTypeID);
                if (sizeChartId <= 0)
                    throw new Exception($"SizeChart not defined for productTypeId [{variant.productTypeID}].");

                var sizeChartWS = await centraSvc.GetSizesForSizeChartAsync(sizeChartId);
                var variantGraph = productGraph.variants.FirstOrDefault(x => x.variantNumber == variant.productVar1Value);

                var canonicalId = 0;
                var categoryIds = new List<int>() { };

                var mappings = centraMapper.GetCentraCanonicalandCategoryIds(variant.productTypeID);
                canonicalId = mappings.Item1 != -1 ? mappings.Item1 : 0;
                categoryIds = new List<int>(mappings.Item2 != null ? mappings.Item2 : null);

                if (string.IsNullOrEmpty(variant.productColorN))
                    throw new Exception($"productColorN empty for [{variant.productMaster}].");

                var variantDto = new ProductVariantDto()
                {
                    CentraId = variantGraph?.id ?? 0,
                    Name = !string.IsNullOrEmpty(variant.productColorN) ? variant.productColorN : productName,
                    CentraSizeChartId = sizeChartId,
                    CentraCanonicalCategoryId = canonicalId,
                    CentraCategoryIds = categoryIds,
                    Status = GetStatusByLifecycle(variant.productLifecycle),
                    VariantNumber = variant.productVar1Value,
                    SeasonCode = variant.productSeasonCode ?? "",
                    InternalName = "",
                    AttributeTypeName = new List<string>()
                    {
                        "season_code",
                        "filter_activity",
                        "filter_climate",
                        "filter_color",
                        "filter_hunting",
                        "filter_layer",
                        "filter_product_property",
                        "filter_season",
                        "filter_technology",
                        "filter_family"
                    },
                    AttributeTypeValue = new List<string>()
                    {
                        variant.productSeasonCode ?? "",
                        JoinArrayWithPipes(variant.productFilterActivityLevel),
                        JoinArrayWithPipes(variant.productFilterClimate),
                        variant.productFilterColor ?? "",
                        JoinArrayWithPipes(variant.productFilterHuntingType),
                        variant.productFilterLayer ?? "",
                        JoinArrayWithPipes(variant.productFilterProductProperty),
                        JoinArrayWithPipes(variant.productFilterSeason),
                        variant.productFilterTechnology ?? "",
                        JoinArrayWithPipes(variant.familyIds)
                    }
                };

                var sizes = products.Where(x => x.productVar1Value == variant.productVar1Value)
                    .ToList();

                variantDto.MediaUrls = new List<string>() { };
                variantDto.ProductSizes = new List<ProductSizeDto>() { };

                foreach (var size in sizes)
                {
                    var centraSizeId = 0;
                    var sizesDto = sizeChartWS.FirstOrDefault()?.sizes;
                    if (sizesDto != null)
                        centraSizeId = sizesDto.FirstOrDefault(x => x.name.ToLower() == size.itemSize?.ToLower())?.id ?? 0;

                    if (string.IsNullOrEmpty(size.itemSize))
                        throw new Exception($"Item size cannot be null for variant: [{variant.productColorN}].");

                    var itemSize = size.itemSize;
                    while (itemSize.IndexOf("\"") >= 0)
                        itemSize = itemSize.Remove(itemSize.IndexOf("\""), 1);

                    var productSize = new ProductSizeDto()
                    {
                        CentraId = variantGraph?.productSizes.FirstOrDefault(x => x.SKU == size.itemSKU)?.id ?? 0,
                        CentraSizeId = centraSizeId,
                        Name = itemSize,
                        SizeNumber = size.itemVar2Value,
                        EAN = string.IsNullOrWhiteSpace(size.itemEan) ? "" : size.itemEan
                    };

                    variantDto.ProductSizes.Add(productSize);

                    foreach (var resId in size.resourceList)
                    {
                        var res = size.resource.FirstOrDefault(x => x.id == resId);
                        if (res != null && !variantDto.MediaUrls.Contains(res.originalUrl))
                            variantDto.MediaUrls.Add(res.originalUrl);
                    }
                }

                product.Variants.Add(variantDto);
            }

            return product;

        }
    }
}
