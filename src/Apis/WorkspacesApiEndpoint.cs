using Faactory.k8s.Models;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;

internal static class WorkspacesApiEndpoints
{
    private static ILogger logger = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
        .CreateLogger( "null" );

    private static bool isInsideWorkspace = false;

    public static IEndpointRouteBuilder MapWorkspacesApi( this IEndpointRouteBuilder builder )
    {
        var ws = builder.MapGroup( "/apis/workspaces" );

        ws.AddEndpointFilter<MetricsEndpointFilter>();

        ws.MapGet( "/", ListWorkspacesAsync );
        ws.MapPost( "/", PostWorkspaceAsync )
            .RequireJsonOrYamlContentType();

        ws.MapGet( "/{name}", GetWorkspaceAsync )
            .WithName( "get-workspace" );
        ws.MapPut( "/{name}", PutWorkspaceAsync )
            .RequireJsonOrYamlContentType();
        ws.MapPatch( "/{name}", PatchWorkspaceAsync )
            .RequireJsonOrYamlContentType();
        ws.MapDelete( "/{name}", DeleteWorkspaceAsync );

        logger = builder.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger( "Apis.Workspaces" );

        var options = builder.ServiceProvider.GetRequiredService<IOptions<GatewayOptions>>().Value;

        isInsideWorkspace = !options.NamespaceDefault().Equals( "default" );

        return ( builder );
    }

    private static async Task<IResult> ListWorkspacesAsync( IKubernetes client )
    {
        if ( isInsideWorkspace )
        {
            // if we are in a workspace, we can't access these resources
            // to save us from an additional request that will return a 403
            // we just return a 204
            return Results.NoContent();
        }

        try
        {
            var workspaces = await client.ListNamespacedAsync<V1Alpha1Workspace>( "faas" );

            return workspaces.Select( x => x.Minimal() )
                .ToHttpResult();
        }
        catch ( k8s.Autorest.HttpOperationException ex )
        {
            return Results.StatusCode( (int)ex.Response.StatusCode );
        }
    }

    private static async Task<IResult> GetWorkspaceAsync( string name, IKubernetes client )
    {
        if ( isInsideWorkspace )
        {
            // if we are in a workspace, we can't access these resources
            // to save us from an additional request that will return a 403
            // we just return a 404
            return Results.NotFound();
        }

        try
        {
            var ws = await client.GetNamespacedAsync<V1Alpha1Workspace>( "faas", name );

            return ws.Sanitize()
                .ToHttpResult();
        }
        catch ( k8s.Autorest.HttpOperationException ex )
        {
            return Results.StatusCode( (int)ex.Response.StatusCode );
        }
    }

    private static async Task<IResult> PostWorkspaceAsync( HttpRequest httpRequest, bool? dryRun, IKubernetes client )
    {
        if ( isInsideWorkspace )
        {
            // if we are in a workspace, we can't access these resources
            // to save us from an additional request that will return a 403
            // we just return a 403
            return Results.StatusCode( 403 );
        }

        var ws = await httpRequest.ReadModelAsync<V1Alpha1Workspace>();

        if ( ws == null )
        {
            return Results.BadRequest();
        }

        // override namespace
        ws.Metadata.SetNamespace( "faas" );

        // verify if workspace already exists
        if ( await client.TryGetNamespacedAsync<V1Alpha1Workspace>( ws.Namespace(), ws.Name() ) != null )
        {
            return Results.Conflict();
        }

        try
        {
            var created = await client.CreateNamespacedAsync(
                ns: ws.Namespace(),
                obj: ws,
                dryRun: dryRun ?? false
            );

            logger.LogObjectCreated<V1Alpha1Workspace>( ws.Name() );

            return Results.CreatedAtRoute( "get-workspace"
                , new
                {
                    name = ws.Name()
                }
                , created.Sanitize() );
        }
        catch ( k8s.Autorest.HttpOperationException ex )
        {
            return Results.StatusCode( (int)ex.Response.StatusCode );
        }
    }

    private static async Task<IResult> PutWorkspaceAsync( HttpRequest httpRequest, string name, bool? dryRun, IKubernetes client )
    {
        if ( isInsideWorkspace )
        {
            // if we are in a workspace, we can't access these resources
            // to save us from an additional request that will return a 403
            // we just return a 403
            return Results.StatusCode( 403 );
        }

        var ws = await httpRequest.ReadModelAsync<V1Alpha1Workspace>();

        if ( ws == null )
        {
            return Results.BadRequest();
        }

        // override namespace
        ws.Metadata.SetNamespace( "faas" );

        try
        {
            var modified = await client.ReplaceNamespacedAsync(
                ns: ws.Namespace(),
                name: name,
                obj: ws,
                dryRun: dryRun ?? false
            );

            logger.LogObjectModified<V1Alpha1Workspace>( name );

            return Results.Ok( modified.Sanitize() );
        }
        catch ( k8s.Autorest.HttpOperationException ex )
        {
            return Results.StatusCode( (int)ex.Response.StatusCode );
        }
    }

    private static async Task<IResult> PatchWorkspaceAsync( HttpRequest httpRequest, string name, bool? dryRun, IKubernetes client )
    {
        if ( isInsideWorkspace )
        {
            // if we are in a workspace, we can't access these resources
            // to save us from an additional request that will return a 403
            // we just return a 403
            return Results.StatusCode( 403 );
        }

        var ws = await httpRequest.ReadModelAsync<V1Alpha1Workspace>();

        if ( ws == null )
        {
            return Results.BadRequest();
        }

        // override namespace
        ws.Metadata.SetNamespace( "faas" );

        try
        {
            var modified = await client.PatchNamespacedAsync(
                ns: ws.Namespace(),
                name: name,
                obj: ws,
                dryRun: dryRun ?? false
            );

            logger.LogObjectModified<V1Alpha1Workspace>( name );

            return Results.Ok( modified.Sanitize() );
        }
        catch ( k8s.Autorest.HttpOperationException ex )
        {
            return Results.StatusCode( (int)ex.Response.StatusCode );
        }
    }

    private static async Task<IResult> DeleteWorkspaceAsync( string name, bool? dryRun, IKubernetes client )
    {
        if ( isInsideWorkspace )
        {
            // if we are in a workspace, we can't access these resources
            // to save us from an additional request that will return a 403
            // we just return a 403
            return Results.StatusCode( 403 );
        }

        try
        {
            await client.DeleteNamespacedAsync<V1Alpha1Workspace>(
                ns: "faas",
                name: name,
                dryRun: dryRun ?? false
            );

            logger.LogObjectDeleted<V1Alpha1Workspace>( name );

            return Results.Accepted();
        }
        catch ( k8s.Autorest.HttpOperationException ex )
        {
            return Results.StatusCode( (int)ex.Response.StatusCode );
        }
    }
}
