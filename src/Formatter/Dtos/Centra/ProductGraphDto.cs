namespace outfit_international.Dtos.Centra
{
    public class CentraProductSizeDto
    {
        public int id { get; set; }
        public string sizeNumber { get; set; }
        public string SKU { get; set; }
    }

    public class CentraProductVariantDto
    {
        public int id { get; set; }
        public string variantNumber { get; set; }
        public CentraProductSizeDto[] productSizes { get; set; }
    }

    public class ProductGraphDto
    {
        public int id { get; set; }
        public CentraProductVariantDto[] variants { get; set; }

        public ProductGraphDto()
        {
            id = 0;
            variants = new CentraProductVariantDto[] { };
        }
    }
}
