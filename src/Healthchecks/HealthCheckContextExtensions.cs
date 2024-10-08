using Microsoft.Extensions.Diagnostics.HealthChecks;

internal static class HealthCheckContextExtensions
{
    public static HealthCheckResult Fail( this HealthCheckContext context, string? description = null, Exception? exception = null )
        => new( context.Registration.FailureStatus, description, exception );
}
