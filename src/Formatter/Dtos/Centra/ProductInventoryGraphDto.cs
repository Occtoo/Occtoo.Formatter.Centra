namespace outfit_international.Dtos.Centra
{
    public class SizeInventoryDto
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class ProductSizeInventoryDto
    {
        public int id { get; set; }
        public string SKU { get; set; }
        public SizeInventoryDto size { get; set; }
    }

    public class VariantInventoryDto
    {
        public int id { get; set; }
        public ProductSizeInventoryDto[] productSizes { get; set; }
    }

    public class ProductInventoryGraphDto
    {
        public int id { get; set; }
        public VariantInventoryDto[] variants { get; set; }
    }
}
