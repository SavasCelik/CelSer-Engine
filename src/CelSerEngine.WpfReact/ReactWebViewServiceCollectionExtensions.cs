using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CelSerEngine.WpfReact;

/// <summary>
/// Extension methods to <see cref="IServiceCollection"/>.
/// </summary>
public static class ReactWebViewServiceCollectionExtensions
{
    /// <summary>
    /// Configures <see cref="IServiceCollection"/> to add support for <see cref="ReactWebView"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddWpfReactWebView(this IServiceCollection services)
    {
        services.TryAddSingleton<ReactJsRuntime>();

        return services;
    }
}
