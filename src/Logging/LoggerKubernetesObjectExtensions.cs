using k8s;
using k8s.Models;

internal static class LoggerKubernetesObjectExtensions
{
    public static void LogObjectCreated( this ILogger logger, string kind, string name )
        => logger.LogInformation( $"{kind}/{name} created." );

    public static void LogObjectModified( this ILogger logger, string kind, string name )
        => logger.LogInformation( $"{kind}/{name} configured." );

    public static void LogObjectUnchanged( this ILogger logger, string kind, string name )
        => logger.LogInformation( $"{kind}/{name} unchanged." );

    public static void LogObjectDeleted( this ILogger logger, string kind, string name )
        => logger.LogInformation( $"{kind}/{name} deleted." );

    public static void LogObjectCreated<T>( this ILogger logger, string name ) where T : IKubernetesObject
        => LogObjectCreated( logger, GetEntityKind<T>(), name );

    public static void LogObjectModified<T>( this ILogger logger, string name ) where T : IKubernetesObject
        => LogObjectModified( logger, GetEntityKind<T>(), name );

    public static void LogObjectUnchanged<T>( this ILogger logger, string name ) where T : IKubernetesObject
        => LogObjectUnchanged( logger, GetEntityKind<T>(), name );

    public static void LogObjectDeleted<T>( this ILogger logger, string name ) where T : IKubernetesObject
        => LogObjectDeleted( logger, GetEntityKind<T>(), name );

    private static string GetEntityKind<T>() where T : IKubernetesObject
    {
        var attr = GetEntityAttribute<T>();

        if ( attr == null )
        {
            // attribute not found! this shouldn't happen, but to avoid
            // throwing an error while logging, we use a fallback method

            return typeof( T ).Name;
        }

        if ( string.IsNullOrEmpty( attr.Group ) )
        {
            return attr.Kind.ToLower();
        }

        return string.Concat( attr.Group, ".", attr.Kind )
            .ToLower();
    }

    private static KubernetesEntityAttribute? GetEntityAttribute<T>() where T : IKubernetesObject
        => GetEntityAttribute( typeof( T ) );

    private static KubernetesEntityAttribute? GetEntityAttribute( Type type )
        => type.GetCustomAttributes( typeof( KubernetesEntityAttribute ), false )
            .Cast<KubernetesEntityAttribute>()
            .SingleOrDefault();
}
