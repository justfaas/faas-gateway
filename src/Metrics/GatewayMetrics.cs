using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Options;
using Prometheus;

internal class GatewayMetrics : IGatewayMetrics
{
    private readonly ( string Name, string Description ) MetricProxyRequestsTotal
        = ( "faas_proxy_requests_total", "Number of requests through the proxy." );

    private readonly ( string Name, string Description ) MetricProxyCompletedRequestsTotal
        = ( "faas_proxy_completed_requests_total", "Number of requests completed through the proxy." );

    private readonly IReadOnlyDictionary<string, string> metrics;

    private readonly string workspaceName;
    private readonly ConcurrentDictionary<string, Counter> counters = new ConcurrentDictionary<string, Counter>();

    public GatewayMetrics( IOptions<GatewayOptions> optionsAccessor, IHttpClientFactory httpClientFactory )
    {
        var options = optionsAccessor.Value;

        workspaceName = options.WorkspaceName;

        metrics = new Dictionary<string, string>
        {
            { MetricProxyRequestsTotal.Name, MetricProxyRequestsTotal.Description }
        };
    }

    public IEnumerable<string> Names() => metrics.Keys;

    public Counter.Child Counter( string metricName, string ns, string functionName )
    {
        if ( !metrics.ContainsKey( metricName ) )
        {
            throw new ArgumentException( "Metric name is not valid!" );
        }

        ( string Name, string Description ) metric = ( metricName, metrics[metricName] );

        return Counter( metric, ns, functionName );
    }

    public Counter.Child GatewayRequestsTotal()
        => ProxyRequestsTotal( workspaceName, "gateway" );

    public Counter.Child GatewayCompletedRequestsTotal()
        => ProxyCompletedRequestsTotal( workspaceName, "gateway" );

    public Counter.Child ProxyRequestsTotal( string ns, string name )
        => Counter( MetricProxyRequestsTotal, ns, name );

    public Counter.Child ProxyCompletedRequestsTotal( string ns, string name )
        => Counter( MetricProxyCompletedRequestsTotal, ns, name );

    private Counter.Child Counter( ( string Name, string Description ) metric, string ns, string name )
    {
        if ( !counters.TryGetValue( metric.Name, out var counter ) )
        {
            counter = Metrics.CreateCounter( 
                metric.Name,
                metric.Description,
                new string[]
                {
                    "namespace",
                    "function"
                }
            );
            
            counters.TryAdd( metric.Name, counter );
        }

        return counter.WithLabels( ns, name );
    }
}
