using System.Text.Json;
using Microsoft.Extensions.Options;

internal static class ProxyRoutes
{
    private const int servicePortDefault = 8080;
    private static string namespaceDefault = "default";
    private static bool isInsideWorkspace = false;

    public static IEndpointRouteBuilder MapProxyRoutes( this IEndpointRouteBuilder builder )
    {
        var proxy = builder.MapGroup( "/proxy" );

        proxy.Map( "/{ns}/{name}/{*all}", SendAsync );
        //proxy.Map( "/{name}/{*all}", SendAsync );

        var options = builder.ServiceProvider.GetRequiredService<IOptions<GatewayOptions>>()
            .Value;

        namespaceDefault = options.NamespaceDefault();
        isInsideWorkspace = !options.NamespaceDefault().Equals( "default" );

        return ( builder );
    }

    private static async Task<IResult> SendAsync( HttpContext httpContext
        , string ns
        , string name
        , ILoggerFactory loggerFactory
        , ProxyHttpClient proxy
        , IGatewayMetrics metrics )
    {
        // set default namespace if undefined
        //ns = ns ?? namespaceDefault;

        if ( isInsideWorkspace && !ns.Equals( namespaceDefault ) )
        {
            // if we are in a workspace, we can't access outside resources
            return Results.StatusCode( 403 );
        }

        // extract path
        var path = httpContext.Request.GetPathWithoutProxy( ns, name );

        var logger = loggerFactory.CreateLogger( $"proxy/{ns}/{name}" );

        if ( !httpContext.Request.Headers.TryGetValue( "X-Service-Port", out var servicePort ) )
        {
            servicePort = $"{servicePortDefault}";
        }
        metrics.GatewayRequestsTotal().Inc();

        try
        {
            var response = await proxy.ExecuteAsync( 
                httpContext.Request,
                ns,
                name,
                servicePort.ToString().ToInt32( servicePortDefault ),
                path,
                CancellationToken.None
            );

            if ( response.StatusCode == System.Net.HttpStatusCode.NoContent )
            {
                return Results.NoContent();
            }

            var content = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.ToString();
            var headers = response.Headers;

            return new CustomHttpResult(
                  statusCode: (int)response.StatusCode
                , contentType: contentType
                , headers: headers
                , content: new ReadOnlyMemory<byte>( content )
            );
        }
        catch ( TimeoutException )
        {
            logger.LogWarning( "Function {ns}/{name} timed out.", ns, name );

            /*
            Timeouts return a 202 response to the caller.
            This is because the function may still be running.
            */

            return Results.Accepted();
        }
        catch ( ConnectionTimeoutException )
        {
            logger.LogWarning( "Unable to connect to {ns}/{name}.", ns, name );
        }
        catch ( Exception ex )
        {
            logger.LogError( ex, ex.Message );
        }
        finally
        {
            // update gateway metrics
            metrics.GatewayCompletedRequestsTotal().Inc();
        }

        return Results.StatusCode( 503 );
    }
}
