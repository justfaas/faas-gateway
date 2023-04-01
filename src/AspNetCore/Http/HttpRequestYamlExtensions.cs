using System.Text;
using Microsoft.Net.Http.Headers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.AspNetCore.Http;

public static class HttpRequestJsonExtensions
{
    private static Lazy<IDeserializer> deserializer = new Lazy<IDeserializer>( 
        () => new DeserializerBuilder()
            .WithNamingConvention( CamelCaseNamingConvention.Instance )
            .IgnoreUnmatchedProperties()
            .Build()
    );

    public static async ValueTask<TValue?> ReadFromYamlAsync<TValue>( this HttpRequest httpRequest, CancellationToken cancellationToken = default )
    {
        ArgumentNullException.ThrowIfNull( httpRequest );

        var mediaType = MediaTypeHeaderValue.Parse( httpRequest.ContentType );
        if ( !mediaType.MatchesMediaType( "text/yaml" ) )
        {
            throw new InvalidOperationException( $"Unable to read the request as YAML because the request content type '{httpRequest.ContentType}' is not a known YAML content type." );
        }

        var ( inputStream, usesTranscodingStream ) = GetInputStream( httpRequest, mediaType.Encoding );

        try
        {
            using ( var reader = new StreamReader( inputStream, leaveOpen: true ) )
            {
                var yaml = await reader.ReadToEndAsync();

                return deserializer.Value.Deserialize<TValue>( yaml );
            }
        }
        finally
        {
            if ( usesTranscodingStream )
            {
                await inputStream.DisposeAsync();
            }
        }
    }

    private static ( Stream inputStream, bool usesTranscodingStream ) GetInputStream( HttpRequest httpRequest, Encoding? encoding )
    {
        if ( encoding == null || encoding.CodePage == Encoding.UTF8.CodePage )
        {
            return ( httpRequest.Body, false  );
        }

        var inputStream = Encoding.CreateTranscodingStream( httpRequest.Body, encoding, Encoding.UTF8, leaveOpen: true );

        return ( inputStream, true );
    }
}
