internal sealed class MetricsEndpointFilter : IEndpointFilter
{
    private readonly IGatewayMetrics metrics;

    public MetricsEndpointFilter( IGatewayMetrics gatewayMetrics )
    {
        metrics = gatewayMetrics;
    }

    public async ValueTask<object?> InvokeAsync( EndpointFilterInvocationContext context, EndpointFilterDelegate next )
    {
        metrics.GatewayRequestsTotal().Inc();

        var result = await next( context );

        metrics.GatewayCompletedRequestsTotal().Inc();

        return ( result );
    }
}
