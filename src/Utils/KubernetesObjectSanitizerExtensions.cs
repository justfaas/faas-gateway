using Faactory.k8s.Models;
using k8s;
using k8s.Models;

internal static class KubernetesObjectSanitizerExtensions
{
    private static readonly string[] ignoredAnnotations = new string[]
    {
        "kubectl.kubernetes.io/last-applied-configuration"
    };

    public static T Sanitize<T>( this T obj ) where T : IMetadata<V1ObjectMeta>
    {
        // strip managed fields
        obj.Metadata.ManagedFields = null;

        if ( obj.Metadata?.Annotations?.Any() == true )
        {
            SanitizeAnnotations( obj );
        }

        return ( obj );
    }

    private static void SanitizeAnnotations<T>( T obj ) where T : IMetadata<V1ObjectMeta>
    {
        obj.Metadata.Annotations = obj.Metadata.Annotations
            .Where( x => !ignoredAnnotations.Contains( x.Key ) )
            .ToDictionary( x => x.Key, x => x.Value );
    }
}
