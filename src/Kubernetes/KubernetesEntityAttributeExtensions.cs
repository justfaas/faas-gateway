using k8s.Models;

internal static class KubernetesEntityAttributeExtensions
{
    public static string GetGroupWithApiVersion( this KubernetesEntityAttribute attr )
    {
        if ( string.IsNullOrEmpty( attr.Group ) )
        {
            return ( attr.ApiVersion );
        }

        return string.Concat( attr.Group, "/", attr.ApiVersion );
    }

    public static string GetPluralName( this KubernetesEntityAttribute attr )
    {
        if ( string.IsNullOrEmpty( attr.PluralName ) )
        {
            return string.Concat( attr.Kind.ToLower(), "s" );
        }

        return attr.PluralName.ToLower();
    }
}
