using System.Collections.Generic;

namespace outfit_international.Model
{
    public class OrchestratorLog
    {
        public List<string> Log { get; set; }

        public OrchestratorLog() =>
            Log = new List<string>() { };
    }
}
