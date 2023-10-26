using System;

namespace outfit_international.Exceptions
{
    public class CentraServerError : Exception
    {
        public CentraServerError() : base("Centra server error.")
        {
        }
    }
}
