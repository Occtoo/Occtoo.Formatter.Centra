namespace outfit_international.Dtos.Centra
{
    public class ProductSizeQuantityDto
    {
        public int id { get; set; }
        public string SKU { get; set; }
    }

    public class StockQuantityDto
    {
        public ProductSizeQuantityDto productSize { get; set; }
        public int availableNowQuantity { get; set; }
        public int physicalQuantity { get; set; }
    }

    public class ProductVariantQuantityDto
    {
        public int id { get; set; }
        public StockQuantityDto[] stock { get; set; }
    }
}
