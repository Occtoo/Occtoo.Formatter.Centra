namespace outfit_international.Dtos.Centra
{
    public class MediaIdDto
    {
        public int id { get; set; }
    }

    public class MediaIdsDto
    {
        public int id { get; set; }
        public MediaIdDto[] media { get; set; }
    }
}
