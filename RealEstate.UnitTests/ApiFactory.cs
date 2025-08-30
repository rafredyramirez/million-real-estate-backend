using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using RealEstate.Application.Interfaces;

namespace RealEstate.UnitTests
{
    public class ApiFactory : WebApplicationFactory<RealEstate.Api.Program>
    {
        public Mock<IPropertyService> ServiceMock { get; } = new(MockBehavior.Strict);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cors:AllowedOrigins:0"] = "http://test-frontend"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPropertyService>();
                services.AddSingleton(ServiceMock.Object);
            });
        }
    }
}
