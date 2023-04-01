/*
Application Builder
*/
using k8s;
using Prometheus;

var builder = WebApplication.CreateBuilder( args );

// configuration
builder.Configuration.AddEnvironmentVariables();

// logging
builder.Logging.ClearProviders()
    .AddSimpleConsole( options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    } )
    .AddFilter( "Microsoft.AspNetCore.Http.Result", LogLevel.Warning )
    .AddFilter( "Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Warning )
    ;

builder.Services.ConfigureHttpJsonOptions( options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
} );

builder.Services.AddHealthChecks()
    .AddCheck<KubernetesHealthCheck>( "kubernetes"
        , Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy
        , new string[] { "kubernetes" } );

builder.Services.AddSingleton<IKubernetes>( provider =>
{
    var config = KubernetesClientConfiguration.IsInCluster()
        ? KubernetesClientConfiguration.InClusterConfig()
        : KubernetesClientConfiguration.BuildConfigFromConfigFile();

    return new Kubernetes( config );
} );

var workspaceName = builder.Configuration["FAAS_WORKSPACE"] ?? string.Empty;

builder.Services.AddGatewayMetrics()
    .Configure<GatewayOptions>( options =>
    {
        if ( !string.IsNullOrEmpty( workspaceName ) )
        {
            options.WorkspaceName = workspaceName;
        }
    } )
    .AddProxyHttpClient();

if ( string.IsNullOrEmpty( workspaceName ) )
{
    // Collector service runs on the non-workspaced gateway (running in 'faas' namespace)
    builder.Services.AddHostedService<CollectorService>();
}

/*
Kestrel
*/
builder.WebHost.ConfigureKestrel( kestrel =>
{
    kestrel.ListenAnyIP( 8080 );
} );

/*
Application Runtime
*/
var app = builder.Build();

Metrics.SuppressDefaultMetrics();

app.MapHealthChecks( "/healthz" );

app.UseMetricServer();

app.MapFunctionsApi()
    #if DEBUG
    //.MapTestApi()
    #endif
    .MapWorkspacesApi()
    .MapSystemApi()
    .MapProxyRoutes()
    .MapProxyAsyncRoutes();

/*
Execution
*/
await app.RunAsync();
