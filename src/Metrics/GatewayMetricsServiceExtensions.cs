namespace Microsoft.Extensions.DependencyInjection;

public static class ProxyMetricsServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayMetrics( this IServiceCollection services )
    {
        services.AddSingleton<IGatewayMetrics, GatewayMetrics>();

        return ( services );
    }
}
