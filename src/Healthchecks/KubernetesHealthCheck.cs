using k8s;
using Microsoft.Extensions.Diagnostics.HealthChecks;

internal sealed class KubernetesHealthCheck : IHealthCheck
{
    private readonly Version minimumVersion = new Version( 1, 23 );
    private readonly IKubernetes client;

    public KubernetesHealthCheck( IKubernetes kubernetes )
    {
        client = kubernetes;
    }

    public async Task<HealthCheckResult> CheckHealthAsync( HealthCheckContext context, CancellationToken cancellationToken = default )
    {
        try
        {
            var response = await client.Version.GetCodeWithHttpMessagesAsync(
                cancellationToken: cancellationToken
            );

            if ( !response.Response.IsSuccessStatusCode )
            {
                return context.Fail( $"Service returned a {(int)response.Response.StatusCode}." );
            }

            var version = new Version(
                major: Int32.Parse( response.Body.Major ),
                minor: Int32.Parse( response.Body.Minor )
            );

            if ( version < minimumVersion )
            {
                return context.Fail( $"Kubernetes server version {version.Major}.{version.Minor} is not supported. Minimum required is version {minimumVersion.Major}.{minimumVersion.Minor}." );
            }

            return HealthCheckResult.Healthy();
        }
        catch ( Exception ex )
        {
            return context.Fail( ex.Message, ex );
        }
    }
}
