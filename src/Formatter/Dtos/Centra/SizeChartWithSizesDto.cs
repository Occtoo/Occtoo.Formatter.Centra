namespace outfit_international.Dtos.Centra
{
    public class SizeDto
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class SizeChartWithSizesDto
    {
        public int id { get; set; }
        public string name { get; set; }
        public SizeDto[] sizes { get; set; }
    }
}
