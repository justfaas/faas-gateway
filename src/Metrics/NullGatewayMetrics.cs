using Prometheus;

internal sealed class NullGatewayMetrics : IGatewayMetrics
{
    public static IGatewayMetrics Instance { get; } = new NullGatewayMetrics();

    private NullGatewayMetrics()
    { }

    public IEnumerable<string> Names()
    {
        throw new NotImplementedException();
    }

    public Counter.Child Counter( string name, string ns, string svc )
    {
        throw new NotImplementedException();
    }

    public Counter.Child GatewayRequestsTotal()
    {
        throw new NotSupportedException();
    }

    public Counter.Child ProxyRequestsTotal( string ns, string name )
    {
        throw new NotSupportedException();
    }

    public Counter.Child GatewayCompletedRequestsTotal()
    {
        throw new NotImplementedException();
    }

    public Counter.Child ProxyCompletedRequestsTotal(string ns, string name)
    {
        throw new NotImplementedException();
    }
}
