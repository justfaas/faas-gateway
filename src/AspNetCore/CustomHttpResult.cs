internal class CustomHttpResult : IResult
{
    private readonly int statusCode;
    private readonly string? contentType;
    private readonly System.Net.Http.Headers.HttpResponseHeaders headers;
    private readonly ReadOnlyMemory<byte> content;

    private readonly string[] unsafeHeaders = new string[]
    {
        "Content-Type",
        "Content-Length",
        "Date",
        "Transfer-Encoding"
    };

    public CustomHttpResult( int statusCode, string? contentType, System.Net.Http.Headers.HttpResponseHeaders headers, ReadOnlyMemory<byte> content )
    {
        this.statusCode = statusCode;
        this.contentType = contentType;
        this.headers = headers;
        this.content = content;
    }

    public async Task ExecuteAsync( HttpContext httpContext )
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = contentType ?? string.Empty;
        httpContext.Response.ContentLength = content.Length;

        foreach ( var header in headers )
        {
            if ( unsafeHeaders.Contains( header.Key, StringComparer.OrdinalIgnoreCase ) )
            {
                continue;
            }

            try
            {
                httpContext.Response.Headers.Append( header.Key, header.Value.ToArray() );
            }
            catch {}
        }

        await httpContext.Response.BodyWriter.WriteAsync( content );
    }
}
