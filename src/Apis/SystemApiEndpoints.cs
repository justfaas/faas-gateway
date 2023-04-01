using Faactory.k8s.Models;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;

internal static class SystemApiEndpoints
{
    private static ILogger logger = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
        .CreateLogger( "null" );

    private static string workspaceName = string.Empty;

    public static IEndpointRouteBuilder MapSystemApi( this IEndpointRouteBuilder builder )
    {
        builder.MapGet( "/apis/system/info", GetInfoAsync )
            .AddEndpointFilter<MetricsEndpointFilter>();

        logger = builder.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger( "Apis.System" );

        var options = builder.ServiceProvider.GetRequiredService<IOptions<GatewayOptions>>().Value;

        if ( !string.IsNullOrEmpty( options.WorkspaceName ) )
        {
            workspaceName = options.WorkspaceName;
        }

        return ( builder );
    }

    private static async Task<IResult> GetInfoAsync( IKubernetes client )
    {
        try
        {
            var kubernetesVersion = await client.Version.GetCodeAsync();

            var gatewayNamespace = string.IsNullOrEmpty( workspaceName )
                ? "faas"
                : workspaceName;

            var gatewayVersion = string.Empty;

            try
            {
                var gateway = await client.GetNamespacedAsync<V1Alpha1Function>( gatewayNamespace, "gateway" );
                gatewayVersion = gateway.Spec.Image!.Split( ':' )
                    .Last();
            }
            catch { }

            return Results.Ok( new
            {
                provider = new
                {
                    name = "kubernetes",
                    version = kubernetesVersion,
                },
                workspace = new
                {
                    name = workspaceName,
                    gatewayVersion = gatewayVersion
                }
            } );
        }
        catch
        {
            return Results.StatusCode( 500 );
        }
    }
}
