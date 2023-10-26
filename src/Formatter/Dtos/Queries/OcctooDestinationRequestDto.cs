using System.Collections.Generic;

namespace outfit_international.Dtos.Queries
{
    public class OcctooPeriodDto
    {
        public string since { get; set; }
        public string before { get; set; }
    }

    public class OcctooFilterDto
    {
        public Dictionary<string, string[]> must { get; set; }
        public Dictionary<string, string[]> mustNot { get; set; }
    }

    public class OcctooDestinationRequestDto
    {
        public bool includeTotals { get; set; }
        public OcctooPeriodDto period { get; set; }
        public int top { get; set; }
        public int skip { get; set; }
        public string[] searchOn { get; set; }
        public string[] search { get; set; }
        public string[] sortAsc { get; set; }
        public string[] sortDesc { get; set; }
        public string[] after { get; set; }
        public bool caseInsensitiveSearch { get; set; }
        public OcctooFilterDto[] filter { get; set; }

        public OcctooDestinationRequestDto(int topRows = 100, int skipRows = 0)
        {
            period = new OcctooPeriodDto() { before = "", since = "" };
            searchOn = new string[] { };
            search = new string[] { };
            sortAsc = new string[] { };
            sortDesc = new string[] { };
            after = new string[] { };
            filter = new OcctooFilterDto[] {
                new OcctooFilterDto() {
                    must = new Dictionary<string, string[]>() { },
                    mustNot = new Dictionary<string, string[]>() { }
                }
            };
            includeTotals = true;
            caseInsensitiveSearch = false;
            top = topRows;
            skip = skipRows;
        }
    }
}
