/// <summary>
/// Ensures the content type matches JSON or YAML content types
/// </summary>
internal sealed class JsonOrYamlEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync( EndpointFilterInvocationContext context, EndpointFilterDelegate next )
    {
        if ( !HasSupportedContentType( context.HttpContext.Request ) )
        {
            return Results.StatusCode( StatusCodes.Status415UnsupportedMediaType );
        }

        return await next( context );
    }

    private bool HasSupportedContentType( HttpRequest httpRequest )
    {
        if ( httpRequest.HasJsonContentType() )
        {
            return ( true );
        }

        var mediaType = Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse( httpRequest.ContentType );

        return mediaType.MatchesMediaType( "text/yaml" );
    }
}

internal static class JsonOrYamlRouteHandlerBuilderExtensions
{
    public static RouteHandlerBuilder RequireJsonOrYamlContentType( this RouteHandlerBuilder builder )
        => builder.AddEndpointFilter<JsonOrYamlEndpointFilter>();
}
