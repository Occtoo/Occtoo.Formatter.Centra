using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using outfit_international;

[assembly: FunctionsStartup(typeof(Startup))]
namespace outfit_international
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
        }
    }
}