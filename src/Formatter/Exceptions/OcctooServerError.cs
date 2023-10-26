using System;

namespace outfit_international.Exceptions
{
    internal class OcctooServerError : Exception
    {
        public OcctooServerError() : base("Occtoo server error.")
        {
        }
    }
}