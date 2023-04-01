namespace Microsoft.Extensions.DependencyInjection;

public static class ProxyHttpClientServiceCollectionExtensions
{
    public static IServiceCollection AddProxyHttpClient( this IServiceCollection services )
    {
        services.AddTransient<ProxyHttpClient>()
            .AddHttpClient( "proxy" )
            .ConfigureHttpClient( httpClient =>
            {
                httpClient.Timeout = TimeSpan.FromMinutes( 10 );
            } );

        return ( services );
    }
}
