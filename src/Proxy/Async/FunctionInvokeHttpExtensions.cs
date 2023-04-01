internal static class FunctionInvokeHttpExtensions
{
    public static async Task<FunctionInvoke> CopyToFunctionInvokeAsync( this HttpRequest httpRequest, string ns, string name )
    {
        var invoke = new FunctionInvoke();

        var path = httpRequest.GetPathWithoutProxy( ns, name );

        // split arguments (if any)
        if ( path.Contains( '?' ) )
        {
            var args = path.Split( '?', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries );

            path = args.First();
            args = args[1].Split( '&', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries );

            foreach ( var arg in args )
            {
                var values = arg.Split( '=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries );

                invoke.Arguments.Add(
                    values.First(),
                    values.Length > 1
                        ? values[1]
                        : string.Empty
                );
            }
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
