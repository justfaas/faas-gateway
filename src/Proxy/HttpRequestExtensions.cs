using System.Net.Http.Headers;

internal static class HttpRequestExtensions
{
    public static async Task<HttpRequestMessage> RewriteProxyRequestAsync( this HttpRequest request, string url )
    {
        // copy query parameters
        if ( request.QueryString.HasValue )
        {
            url = string.Concat(
                url,
                request.QueryString.ToString()
            );
        }

        var message = new HttpRequestMessage( new HttpMethod( request.Method ), url );

        // copy headers
        foreach ( var header in request.Headers )
        {
            try
            {
                message.Headers.Add( header.Key, header.Value.ToArray() );
            }
            catch { }
        }

        // copy message body
        var content = await request.BodyReader.ReadAsByteArrayAsync();
        
        message.Content = new ByteArrayContent( content );
        message.Content.Headers.ContentLength = request.ContentLength;

        if ( request.ContentType != null )
        {
            message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse( request.ContentType );
        }

        return ( message );
    }

    public static string GetPathWithoutProxy( this HttpRequest request, string? ns, string name )
    {
        var prefix = request.Path.ToString().StartsWith( "/proxy-async/" )
            ? "proxy-async"
            : "proxy";

        if ( ( ns != null ) && ( request.Path.ToString().StartsWith( $"/{prefix}/{ns}/{name}" ) ) )
        {
            return request.Path.ToString()
                .Substring( $"/{prefix}/{ns}/{name}".Length )
                .EnsureEndsWith( "/" );
        }

        return request.Path.ToString()
            .Substring( $"/{prefix}/{name}".Length )
            .EnsureEndsWith( "/" );
    }
}
