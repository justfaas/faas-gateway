using Prometheus;

public interface IGatewayMetrics
{
    IEnumerable<string> Names();
    Counter.Child Counter( string metricName, string ns, string functionName );
    Counter.Child GatewayRequestsTotal();
    Counter.Child GatewayCompletedRequestsTotal();
    Counter.Child ProxyRequestsTotal( string ns, string name );
    Counter.Child ProxyCompletedRequestsTotal( string ns, string name );
}
