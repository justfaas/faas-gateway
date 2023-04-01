using Microsoft.Net.Http.Headers;

internal static class HttpRequestMediaTypeExtensions
{
    public static ValueTask<TValue?> ReadModelAsync<TValue>( this HttpRequest httpRequest, CancellationToken cancellationToken = default )
    {
        var mediaType = MediaTypeHeaderValue.Parse( httpRequest.ContentType );

        if ( mediaType.MatchesMediaType( "text/yaml" ) )
        {
            return httpRequest.ReadFromYamlAsync<TValue>( cancellationToken );
        }

        return httpRequest.ReadFromJsonAsync<TValue>( cancellationToken );
    }
}
