internal class CustomHttpResult : IResult
{
    private readonly int statusCode;
    private readonly string? contentType;
    private readonly ReadOnlyMemory<byte> content;

    public CustomHttpResult( int statusCode, string? contentType, ReadOnlyMemory<byte> content )
    {
        this.statusCode = statusCode;
        this.contentType = contentType;
        this.content = content;
    }

    public async Task ExecuteAsync( HttpContext httpContext )
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = contentType ?? string.Empty;
        httpContext.Response.ContentLength = content.Length;

        await httpContext.Response.BodyWriter.WriteAsync( content );
    }
}
