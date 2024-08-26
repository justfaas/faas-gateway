using System.Text;
using Faactory.k8s.Models;
using k8s;
using k8s.Models;

internal sealed class CollectorService : BackgroundService
{
    private readonly ILogger logger;
    private readonly IKubernetes kubeClient;
    private readonly HttpClient httpClient;
    private readonly IGatewayMetrics metrics;

    public CollectorService( ILoggerFactory loggerFactory
        , IKubernetes kubernetes
        , IHttpClientFactory httpClientFactory
        , IGatewayMetrics gatewayMetrics )
    {
        logger = loggerFactory.CreateLogger<CollectorService>();
        kubeClient = kubernetes;
        httpClient = httpClientFactory.CreateClient();
        metrics = gatewayMetrics;
    }

    public override Task StartAsync( CancellationToken cancellationToken )
    {
        logger.LogInformation( "Started." );

        return base.StartAsync( cancellationToken );
    }

    public override Task StopAsync( CancellationToken cancellationToken )
    {
        logger.LogInformation( "Stopped." );

        return base.StopAsync( cancellationToken );
    }

    protected override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        while ( !stoppingToken.IsCancellationRequested )
        {
            await Task.Delay( 2000, stoppingToken );

            if ( stoppingToken.IsCancellationRequested )
            {
                continue;
            }

            try
            {
                var workspaces = await kubeClient.ListNamespacedAsync<V1Alpha1Workspace>( "faas" );

                if ( !workspaces.Any() )
                {
                    // delay next attempt if there aren't any active namespaces
                    await Task.Delay( 10000, stoppingToken );
                    continue;
                }

                var tasks = workspaces.Select( w => CollectAsync( w ) )
                    .ToArray();

                await Task.WhenAll( tasks );
            }
            catch ( k8s.Autorest.HttpOperationException ex )
            {
                logger.LogWarning( "Failed to list workspaces on 'faas' namespace. {Error}", ex.Message );

                // delay next attempt
                await Task.Delay( 10000, stoppingToken );
            }
        }
    }

    private async Task CollectAsync( V1Alpha1Workspace workspace )
    {
        try
        {
            var prometheusData = await httpClient.GetStringAsync( $"http://gateway.{workspace.Name()}.svc.cluster.local:8080/metrics" );

            if ( string.IsNullOrEmpty( prometheusData ) )
            {
                return;
            }

            var items = PrometheusParser.Parse( prometheusData )
                .Where( x => metrics.Names().Contains( x.Metric ) ) // only supported metrics
                .ToArray();

            foreach ( var item in items )
            {
                var ns = item.Labels.GetValueOrDefault( "namespace" );
                var name = item.Labels.GetValueOrDefault( "function" );

                if ( ( ns == null ) || ( name == null ) )
                {
                    // ignore bad data
                    continue;
                }

                // currently only supports counters
                metrics.Counter( item.Metric, ns, name )
                    .IncTo( item.Value );
            }
        }
        catch ( Exception ex )
        {
            logger.LogWarning( "Failed to collect metrics for workspace '{workspace}'. {Error}", workspace.Name(), ex.Message );
        }
    }
}
