// using Faactory.k8s.Models;
// using CloudNative.CloudEvents.AspNetCore;
// using k8s;
// using k8s.Models;
// using Microsoft.Extensions.Options;
// using CloudNative.CloudEvents;
// using Microsoft.Net.Http.Headers;

// internal static class TestApiEndpoints
// {
//     private static ILogger logger = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
//         .CreateLogger( "null" );

//     public static IEndpointRouteBuilder MapTestApi( this IEndpointRouteBuilder builder )
//     {
//         builder.MapPost( "/apis/test/yaml", PostYamlAsync )
//             .AddEndpointFilter<MetricsEndpointFilter>()
//             .RequireJsonOrYamlContentType();

//         logger = builder.ServiceProvider.GetRequiredService<ILoggerFactory>()
//             .CreateLogger( "Apis.Test" );

//         return ( builder );
//     }

//     private static readonly CloudEventFormatter formatter = new CloudNative.CloudEvents.SystemTextJson.JsonEventFormatter();

//     private static async Task<IResult> PostYamlAsync( HttpRequest httpRequest )
//     {
//         await Task.Delay( 0 );

//         var function = await httpRequest.ReadModelAsync<V1Alpha1Function>();

//         return Results.Json( function );
//     }
// }
