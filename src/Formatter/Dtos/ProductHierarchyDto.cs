using System.Collections.Generic;

namespace outfit_international.Dtos
{
    public class ProductSizeDto
    {
        public int CentraId { get; set; }
        public int CentraSizeId { get; set; }
        public string Name { get; set; }
        public string EAN { get; set; }
        public string SizeNumber { get; set; }
    }

    public class ProductVariantDto
    {
        public int CentraId { get; set; }
        public string Name { get; set; }
        public int CentraSizeChartId { get; set; }
        public int CentraCanonicalCategoryId { get; set; }
        public List<int> CentraCategoryIds { get; set; }
        public string Status { get; set; }
        public string VariantNumber { get; set; }
        public string SeasonCode { get; set; }
        public string InternalName { get; set; }
        public List<string> AttributeTypeName { get; set; }
        public List<string> AttributeTypeValue { get; set; }
        public List<string> MediaUrls { get; set; }
        public List<ProductSizeDto> ProductSizes { get; set; }
    }

    public class ProductHierarchyDto
    {
        public int CentraId { get; set; }
        public int BrandId { get; set; }
        public int CollectionId { get; set; }
        public int FolderId { get; set; }
        public int MeasurementChartId { get; set; }
        public string Name { get; set; }
        public string ProductNumber { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string InternalComment { get; set; }
        public string Weight { get; set; }
        public List<string> ExcludedFromMarkets { get; set; }
        public List<ProductVariantDto> Variants { get; set; }
    }
}
