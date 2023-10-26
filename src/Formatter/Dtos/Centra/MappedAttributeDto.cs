namespace outfit_international.Dtos.Centra
{
    public class MappedAttributeTypeDto
    {
        public string name { get; set; }
    }

    public class MappedAttributeElementDto
    {
        public string key { get; set; }
        public string value { get; set; }
    }

    public class MappedAttributeDto
    {
        public MappedAttributeTypeDto type { get; set; }
        public MappedAttributeElementDto[] elements { get; set; }
    }

    public class MappedAttributesDto
    {
        public MappedAttributeDto[] attributes { get; set; }
    }
}
