using System;

namespace outfit_international.Helpers
{
    public static class Settings
    {
        public static string OcctooUrl =>
            Environment.GetEnvironmentVariable("OcctooUrl");

        public static string OcctooUrlToken =>
            Environment.GetEnvironmentVariable("OcctooUrlToken");

        public static string OcctooClientId =>
            Environment.GetEnvironmentVariable("OcctooClientId");

        public static string OcctooClientSecret =>
            Environment.GetEnvironmentVariable("OcctooClientSecret");

        public static string EnvironmentUrl =>
            Environment.GetEnvironmentVariable("EnvironmentUrl");

        public static string EnvironmentToken =>
            Environment.GetEnvironmentVariable("EnvironmentToken");
    }
}
