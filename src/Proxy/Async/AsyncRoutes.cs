using System.Text.Json;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Http;
using Microsoft.Extensions.Options;

internal static class ProxyAsyncRoutes
{
    private static string namespaceDefault = "default";
    private static bool isInsideWorkspace = false;
    private static readonly CloudEventFormatter formatter = new CloudNative.CloudEvents.SystemTextJson.JsonEventFormatter();

    public static IEndpointRouteBuilder MapProxyAsyncRoutes( this IEndpointRouteBuilder builder )
    {
        var proxy = builder.MapGroup( "/proxy-async" );

        proxy.AddEndpointFilter<MetricsEndpointFilter>();

        proxy.Map( "/{ns}/{name}/{*all}", PublishAsync );
        proxy.Map( "/{name}/{*all}", PublishAsync );

        var options = builder.ServiceProvider.GetRequiredService<IOptions<GatewayOptions>>()
            .Value;

        namespaceDefault = options.NamespaceDefault();
        isInsideWorkspace = !options.NamespaceDefault().Equals( "default" );

        return ( builder );
    }

    private static async Task<IResult> PublishAsync( HttpRequest httpRequest
        , string? ns
        , string name
        , ILoggerFactory loggerFactory
        , IHttpClientFactory httpClientFactory
        , IGatewayMetrics metrics )
    {
        ns = ns ?? namespaceDefault;

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

            var body = await httpRequest.CopyToFunctionInvokeAsync( ns, name );

            var cloudEvent = new CloudEvent
            {
                Id = Guid.NewGuid().ToString( "N" ),
                Source = new Uri( "http://gateway.faas.svc.cluster.local:8080/cloudevents/spec/function" ),
                Type = "com.justfaas.function.invoked",
                Subject = $"{ns}/{name}",
                Data = body,
                DataContentType = "application/json",
            };

            if ( httpRequest.Headers.TryGetValue( "X-Webhook-Url", out var webhookUrl ) )
            {
                cloudEvent.SetAttributeFromString( "webhookurl", webhookUrl.ToString() );
            }

            var httpContent = cloudEvent.ToHttpContent( ContentMode.Structured, formatter );

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
