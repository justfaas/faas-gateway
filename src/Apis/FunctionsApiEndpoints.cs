using Faactory.k8s.Models;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;

internal static class FunctionsApiEndpoints
{
    private static ILogger logger = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
        .CreateLogger( "null" );

    private static string namespaceDefault = "default";
    private static bool isInsideWorkspace = false;

    public static IEndpointRouteBuilder MapFunctionsApi( this IEndpointRouteBuilder builder )
    {
        var apps = builder.MapGroup( "/apis/functions" );

        apps.AddEndpointFilter<MetricsEndpointFilter>();

        apps.MapGet( "/", ListAppsAsync );
        apps.MapPost( "/", PostAppAsync )
            .RequireJsonOrYamlContentType();

        apps.MapGet( "/{ns}/{name}", GetFunctionAsync )
            .WithName( "get-namespaced" );
        apps.MapGet( "/{name}", GetFunctionAsync );

        apps.MapPut( "/{ns}/{name}", PutAppAsync )
            .RequireJsonOrYamlContentType();
        apps.MapPut( "/{name}", PutAppAsync )
            .RequireJsonOrYamlContentType();

        apps.MapPatch( "/{ns}/{name}", PatchAppAsync )
            .RequireJsonOrYamlContentType();
        apps.MapPatch( "/{name}", PatchAppAsync )
            .RequireJsonOrYamlContentType();

        apps.MapDelete( "/{ns}/{name}", DeleteAppAsync );
        apps.MapDelete( "/{name}", DeleteAppAsync );

        logger = builder.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger( "Apis.Functions" );

        var options = builder.ServiceProvider.GetRequiredService<IOptions<GatewayOptions>>()
            .Value;

        namespaceDefault = options.NamespaceDefault();
        isInsideWorkspace = !options.NamespaceDefault().Equals( "default" );

        return ( builder );
    }

    private static async Task<IResult> ListAppsAsync( string? ns, IKubernetes client )
    {
        ns = ns ?? namespaceDefault;

        if ( isInsideWorkspace && !ns.Equals( namespaceDefault ) )
        {
            // if we are in a workspace, we can't access outside resources
            // to save us from an additional request that will return a 403
            // we just return a 204
            return Results.NoContent();
        }

        try
        {
            var functions = await client.ListNamespacedAsync<V1Alpha1Function>( ns );

            // discard 'faas' deployed apps
            return functions.Where( x => !x.Labels().ContainsKeyWithValue( AppLabels.PartOf, "faas" ) )
                .Select( x => x.Minimal() )
                .ToHttpResult();
        }
        catch ( k8s.Autorest.HttpOperationException ex )
        {
            return Results.StatusCode( (int)ex.Response.StatusCode );
        }
    }

    private static async Task<IResult> GetFunctionAsync( string? ns, string name, IKubernetes client )
    {
        ns = ns ?? namespaceDefault;

        if ( isInsideWorkspace && !ns.Equals( namespaceDefault ) )
        {
            // if we are in a workspace, we can't access outside resources
            // to save us from an additional request that will return a 403
            // we just return a 404
            return Results.NotFound();
        }

        try
        {
            var function = await client.GetNamespacedAsync<V1Alpha1Function>( ns, name );

            // forbid 'faas' deployed apps
            if ( function.Labels().ContainsKeyWithValue( AppLabels.PartOf, "faas" ) )
            {
                return Results.StatusCode( 403 );
            }

            return function.Sanitize()
                .ToHttpResult();
        }
        catch ( k8s.Autorest.HttpOperationException ex )
        {
            return Results.StatusCode( (int)ex.Response.StatusCode );
        }
    }

    private static async Task<IResult> PostAppAsync( HttpRequest httpRequest, bool? dryRun, IKubernetes client )
    {
        var function = await httpRequest.ReadModelAsync<V1Alpha1Function>();

        if ( function == null )
        {
            return Results.BadRequest();
        }

        // override/ensure namespace
        function.Metadata.SetNamespace( function.Namespace() ?? namespaceDefault );

        if ( isInsideWorkspace && !function.Namespace().Equals( namespaceDefault ) )
        {
            // if we are in a workspace, we can't access outside resources
            // to save us from an additional request that will return a 403
            // we just return a 403
            return Results.StatusCode( 403 );
        }

        // verify if app already exists
        if ( await client.TryGetNamespacedAsync<V1Alpha1Function>( function.Namespace(), function.Name() ) != null )
        {
            return Results.Conflict();
        }

        try
        {
            var created = await client.CreateNamespacedAsync(
                ns: function.Namespace(),
                obj: function,
                dryRun: dryRun ?? false
            );

            logger.LogObjectCreated<V1Alpha1Function>( function.Name() );

            return Results.CreatedAtRoute( "get-namespaced"
                , new
                {
                    ns = created.Namespace(),
                    name = created.Name()
                }
                , created.Sanitize() );
        }
        catch ( k8s.Autorest.HttpOperationException ex )
        {
            return Results.StatusCode( (int)ex.Response.StatusCode );
        }
    }

    private static async Task<IResult> PutAppAsync( HttpRequest httpRequest, string? ns, string name, bool? dryRun, IKubernetes client )
    {
        ns = ns ?? namespaceDefault;

        if ( isInsideWorkspace && !ns.Equals( namespaceDefault ) )
        {
            // if we are in a workspace, we can't access outside resources
            // to save us from an additional request that will return a 403
            // we just return a 403
            return Results.StatusCode( 403 );
        }

        var function = await httpRequest.ReadModelAsync<V1Alpha1Function>();

        if ( function == null )
        {
            return Results.BadRequest();
        }

        // override namespace
        function.Metadata.SetNamespace( ns );

        // verify if existing app can be modified
        var existing = await client.TryGetNamespacedAsync<V1Alpha1Function>( ns, name );

        if ( existing?.Labels().ContainsKeyWithValue( AppLabels.PartOf, "faas" ) == true )
        {
            return Results.StatusCode( 403 );
        }

        try
        {
            var modified = await client.ReplaceNamespacedAsync(
                ns: ns,
                name: name,
                obj: function,
                dryRun: dryRun ?? false
            );

            logger.LogObjectModified<V1Alpha1Function>( name );

            return Results.Ok( modified.Sanitize() );
        }
        catch ( k8s.Autorest.HttpOperationException ex )
        {
            return Results.StatusCode( (int)ex.Response.StatusCode );
        }
    }

    private static async Task<IResult> PatchAppAsync( HttpRequest httpRequest, string? ns, string name, bool? dryRun, IKubernetes client )
    {
        ns = ns ?? namespaceDefault;

        if ( isInsideWorkspace && !ns.Equals( namespaceDefault ) )
        {
            // if we are in a workspace, we can't access outside resources
            // to save us from an additional request that will return a 403
            // we just return a 403
            return Results.StatusCode( 403 );
        }

        var function = await httpRequest.ReadModelAsync<V1Alpha1Function>();

        if ( function == null )
        {
            return Results.BadRequest();
        }

        // override namespace
        function.Metadata.SetNamespace( ns );

        // verify if existing app can be modified
        var existing = await client.TryGetNamespacedAsync<V1Alpha1Function>( ns, name );

        if ( existing?.Labels().ContainsKeyWithValue( AppLabels.PartOf, "faas" ) == true )
        {
            return Results.StatusCode( 403 );
        }

        try
        {
            var modified = await client.PatchNamespacedAsync(
                ns: ns,
                name: name,
                obj: function,
                dryRun: dryRun ?? false
            );

            logger.LogObjectModified<V1Alpha1Function>( name );

            return Results.Ok( modified.Sanitize() );
        }
        catch ( k8s.Autorest.HttpOperationException ex )
        {
            return Results.StatusCode( (int)ex.Response.StatusCode );
        }
    }

    private static async Task<IResult> DeleteAppAsync( string? ns, string name, bool? dryRun, IKubernetes client )
    {
        ns = ns ?? namespaceDefault;

        if ( isInsideWorkspace && !ns.Equals( namespaceDefault ) )
        {
            // if we are in a workspace, we can't access outside resources
            // to save us from an additional request that will return a 403
            // we just return a 403
            return Results.StatusCode( 403 );
        }

        // verify if existing app can be deleted
        var existing = await client.TryGetNamespacedAsync<V1Alpha1Function>( ns, name );

        if ( existing?.Labels().ContainsKeyWithValue( AppLabels.PartOf, "faas" ) == true )
        {
            return Results.StatusCode( 403 );
        }

        try
        {
            await client.DeleteNamespacedAsync<V1Alpha1Function>(
                ns: ns,
                name: name,
                dryRun: dryRun ?? false
            );

            logger.LogObjectDeleted<V1Alpha1Function>( name );

            return Results.Accepted();
        }
        catch ( k8s.Autorest.HttpOperationException ex )
        {
            return Results.StatusCode( (int)ex.Response.StatusCode );
        }
    }
}
