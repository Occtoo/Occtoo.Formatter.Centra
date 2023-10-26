namespace outfit_international.Dtos.Occtoo
{
    /// <summary>
    /// Your DTO will differ to this depending on what properties you expose in your Occtoo API
    /// This is just a example
    /// </summary>
    public class MediaDto
    {
        public string id { get; set; }
        public string originalUrl { get; set; }
    }

    public class ProductDto
    {
        public string id { get; set; }
        public string productName { get; set; }
        public string productBCBrand { get; set; }
        public string productTypeID { get; set; }
        public string productShortWebText { get; set; }
        public string productColorN { get; set; }
        public string itemWeight { get; set; }
        public string productSeasonCode { get; set; }
        public string[] productExcludedMarkets { get; set; }
        public string[] familyIds { get; set; }
        public string[] productFilterActivityLevel { get; set; }
        public string[] productFilterClimate { get; set; }
        public string productFilterColor { get; set; }
        public string[] productFilterHuntingType { get; set; }
        public string productFilterLayer { get; set; }
        public string[] productFilterProductProperty { get; set; }
        public string[] productFilterSeason { get; set; }
        public string productFilterTechnology { get; set; }
        public string productMaster { get; set; }
        public string productVar1Value { get; set; }
        public string itemVar2Value { get; set; }
        public string itemSize { get; set; }
        public string itemSKU { get; set; }
        public string itemEan { get; set; }
        public string productLifecycle { get; set; }
        public string[] resourceList { get; set; }
        public string productBcStatusCode { get; set; }
        public MediaDto[] resource { get; set; }
    }
}
