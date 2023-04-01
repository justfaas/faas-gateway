internal static class HttpResultObjectExtensions
{
    public static IResult ToHttpResult<T>( this IEnumerable<T> source )
    {
        if ( !source.Any() )
        {
            return Results.NoContent();
        }

        return Results.Ok( source );
    }

    public static IResult ToHttpResult( this object? obj )
    {
        if ( obj == null )
        {
            return Results.NotFound();
        }

        return Results.Ok( obj );
    }
}
