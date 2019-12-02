using Datory;
using Datory.Cli.Abstractions;
using Datory.Cli.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ISettings AddSettings(this IServiceCollection services, IConfiguration configuration, string contentRootPath)
        {
            var settings = new Settings(configuration, contentRootPath);
            services.TryAdd(ServiceDescriptor.Singleton<ISettings>(settings));

            return settings;
        }
    }
}
