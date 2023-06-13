using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

internal static class ProxyAsyncRoutes
{
    private static string namespaceDefault = "default";
    private static bool isInsideWorkspace = false;

    public static IEndpointRouteBuilder MapProxyAsyncRoutes( this IEndpointRouteBuilder builder )
    {
        var proxy = builder.MapGroup( "/proxy-async" );

        proxy.AddEndpointFilter<MetricsEndpointFilter>();

        proxy.Map( "/{ns}/{name}/{*all}", PublishAsync );
        //proxy.Map( "/{name}/{*all}", PublishAsync );

        var options = builder.ServiceProvider.GetRequiredService<IOptions<GatewayOptions>>()
            .Value;

        namespaceDefault = options.NamespaceDefault();
        isInsideWorkspace = !options.NamespaceDefault().Equals( "default" );

        return ( builder );
    }

    private static async Task<IResult> PublishAsync( HttpRequest httpRequest
        , string ns
        , string name
        , ILoggerFactory loggerFactory
        , IHttpClientFactory httpClientFactory
        , IGatewayMetrics metrics )
    {
        //ns = ns ?? namespaceDefault;

        if ( isInsideWorkspace && !ns.Equals( namespaceDefault ) )
        {
            // if we are in a workspace, we can't access outside resources
            return Results.StatusCode( 403 );
        }

        var path = httpRequest.GetPathWithoutProxy( ns, name );

        var logger = loggerFactory.CreateLogger( $"proxy-async/{ns}/{name}" );

        /*
        Increment metrics when publishing an async requests.
        This speeds up scaling from zero for async requests.

        This does create a disruption between requests and completed requests,
        since there is no completed request for this desired request.
        */
        metrics.ProxyRequestsTotal( ns, name ).Inc();

        try
        {
            var httpClient = httpClientFactory.CreateClient();

            var functionCall = await httpRequest.CopyToFunctionCallAsync( ns, name );

            var json = JsonSerializer.Serialize( functionCall );
            var httpContent = new StringContent( json, Encoding.UTF8, "application/json" );
            var gatewayNamespace = isInsideWorkspace ? namespaceDefault : "faas";

            httpContent.Headers.Add( "X-Event-Source", $"http://gateway.{gatewayNamespace}" );
            httpContent.Headers.Add( "X-Event-Type", "com.justfaas.function.invoked" );

            if ( httpRequest.Headers.TryGetValue( "X-Webhook-Url", out var webhookUrl ) )
            {
                httpContent.Headers.Add( "X-Event-Webhook-Url", webhookUrl.ToString() );
            }

            await httpClient.PostAsync( $"http://events.faas.svc.cluster.local:8080/apis/events", httpContent );

            return Results.Accepted();
        }
        catch ( Exception ex )
        {
            logger.LogError( ex, ex.Message );

            return Results.Content( ex.Message, statusCode: 500 );
        }
    }
}
