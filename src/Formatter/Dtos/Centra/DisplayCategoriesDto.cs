namespace outfit_international.Dtos.Centra
{
    public class DisplayCanonicalCategoryDto
    {
        public int id { get; set; }
    }

    public class DisplayCategoryDto
    {
        public int id { get; set; }
    }

    public class DisplayCategoriesDto
    {
        public DisplayCanonicalCategoryDto canonicalCategory { get; set; }
        public DisplayCategoryDto[] categories { get; set; }
    }
}
