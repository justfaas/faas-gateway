public sealed class GatewayOptions
{
    /// <summary>
    /// The namespace where the gateway is running. Injected by FAAS_WORKSPACE environment variable.
    /// </summary>
    public string WorkspaceName { get; set; } = string.Empty;
}

internal static class GatewayOptionsExtensions
{
    /// <summary>
    /// Gets the default namespace for deploying functions
    /// </summary>
    public static string NamespaceDefault( this GatewayOptions options )
        => string.IsNullOrEmpty( options.WorkspaceName )
            ? "default"
            : options.WorkspaceName;
}
