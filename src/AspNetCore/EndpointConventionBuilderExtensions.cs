namespace Microsoft.AspNetCore.Builder;

public static class EndpointConventionBuilderLocalExtensions
{
    public static IEndpointConventionBuilder RequireLocalPort( this IEndpointConventionBuilder builder, int port )
        => builder.AddEndpointFilter( async ( context, next ) =>
        {
            if ( context.HttpContext.Connection.LocalPort != port )
            {
                return Results.NotFound();
            }

            return await next( context );
        } );
}
