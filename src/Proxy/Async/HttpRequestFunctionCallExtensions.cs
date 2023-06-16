internal static class HttpRequestFunctionCallExtensions
{
    public static async Task<FunctionCall> CopyToFunctionCallAsync( this HttpRequest httpRequest, string ns, string name )
    {
        var invoke = new FunctionCall
        {
            Namespace = ns,
            Name = name
        };

        var path = httpRequest.GetPathWithoutProxy( ns, name );

        // copy arguments (if any)
        foreach ( var arg in httpRequest.Query )
        {
            invoke.Arguments.Add(
                arg.Key,
                arg.Value.ToString()
            );
        }

        invoke.Metadata.Add( "HTTP_METHOD", httpRequest.Method );
        invoke.Metadata.Add( "HTTP_PATH", path );

        // copy headers
        foreach ( var header in httpRequest.Headers )
        {
            invoke.Metadata.Add( $"HTTP_HEADER_{header.Key}", header.Value.ToString() );

            if ( header.Key.Equals( "Authorization", StringComparison.OrdinalIgnoreCase ) )
            {
                invoke.Metadata.Add( "Authorization", header.Value.ToString() );
            }
        }

        // copy cookies
        foreach ( var cookie in httpRequest.Cookies )
        {
            invoke.Metadata.Add( $"HTTP_COOKIE_{cookie.Key}", cookie.Value );
        }

        invoke.ContentType = httpRequest.ContentType;
        invoke.Content = await httpRequest.BodyReader.ReadAsByteArrayAsync();

        return ( invoke );
    }
}
