namespace tests;

public class PrometheusTests
{
    [Fact]
    public void TestParser()
    {
        var text =
"""
# HELP faas_proxy_requests_total Number of requests over the proxy.
# TYPE faas_proxy_requests_total counter
faas_proxy_requests_total{namespace="faas",function="gateway"} 1
""";

        var items = PrometheusParser.Parse( text );

        var item = Assert.Single( items );

        Assert.Equal( "faas_proxy_requests_total", item.Metric );
        Assert.Equal( 2, item.Labels.Count );
        Assert.Equal( "faas", item.Labels["namespace"] );
        Assert.Equal( "gateway", item.Labels["function"] );
        Assert.Equal( 1UL, item.Value );
    }
}
