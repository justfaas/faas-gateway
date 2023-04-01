using k8s;
using k8s.Models;

public static class KubernetesClientGenericExtensions
{
    public static async Task<IList<T>> ListAsync<T>( this IKubernetes client
        , string? labelSelector = null )
    where T : IKubernetesObject<V1ObjectMeta>
    {
        var attr = typeof( T )
            .GetKubernetesEntityAttribute();

        var result = await client.CustomObjects.ListClusterCustomObjectWithHttpMessagesAsync(
            group: attr.Group,
            version: attr.ApiVersion,
            plural: attr.GetPluralName(),
            labelSelector: labelSelector
        );

        var list = KubernetesJson.Deserialize<GenericList<T>>( result.Body.ToString() );

        return ( list.Items );
    }

    public static async Task<IList<T>> ListNamespacedAsync<T>( this IKubernetes client
        , string ns
        , string? labelSelector = null )
    where T : IKubernetesObject<V1ObjectMeta>
    {
        var attr = typeof( T )
            .GetKubernetesEntityAttribute();

        var result = await client.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(
            group: attr.Group,
            version: attr.ApiVersion,
            namespaceParameter: ns,
            plural: attr.GetPluralName(),
            labelSelector: labelSelector
        );

        var list = KubernetesJson.Deserialize<GenericList<T>>( result.Body.ToString() );

        return ( list.Items );
    }

    public static async Task<T?> TryGetNamespacedAsync<T>( this IKubernetes client
        , string ns
        , string name )
    where T : class, IKubernetesObject<V1ObjectMeta>
    {
        try
        {
            return await GetNamespacedAsync<T>( client, ns, name );
        }
        catch ( k8s.Autorest.HttpOperationException )
        {
            return ( null );
        }
    }

    public static async Task<T> GetNamespacedAsync<T>( this IKubernetes client
        , string ns
        , string name )
    where T : IKubernetesObject<V1ObjectMeta>
    {
        var attr = typeof( T )
            .GetKubernetesEntityAttribute();

        var response = await client.CustomObjects.GetNamespacedCustomObjectWithHttpMessagesAsync(
            group: attr.Group,
            version: attr.ApiVersion,
            namespaceParameter: ns,
            plural: attr.GetPluralName(),
            name: name
        );

        var obj = KubernetesJson.Deserialize<T>( response.Body.ToString() );

        return ( obj );
    }

    public static async Task<T> CreateNamespacedAsync<T>( this IKubernetes client
        , string ns
        , T obj
        , bool dryRun = false )
    where T : IKubernetesObject<V1ObjectMeta>
    {
        var attr = typeof( T ).GetKubernetesEntityAttribute();

        obj.Kind = attr.Kind;
        obj.ApiVersion = attr.GetGroupWithApiVersion();

        var response = await client.CustomObjects.CreateNamespacedCustomObjectWithHttpMessagesAsync(
            body: obj,
            group: attr.Group,
            version: attr.ApiVersion,
            namespaceParameter: ns,
            plural: attr.GetPluralName(),
            dryRun: dryRun ? "All" : null
        );

        var created = k8s.KubernetesJson.Deserialize<T>( response.Body.ToString() );

        return ( created );
    }

    public static async Task<T> ReplaceNamespacedAsync<T>( this IKubernetes client
        , string ns
        , string name
        , T obj
        , bool dryRun = false )
    where T : IKubernetesObject<V1ObjectMeta>
    {
        var attr = typeof( T )
            .GetKubernetesEntityAttribute();

        var response = await client.CustomObjects.ReplaceNamespacedCustomObjectWithHttpMessagesAsync(
            body: obj,
            group: attr.Group,
            version: attr.ApiVersion,
            namespaceParameter: ns,
            plural: attr.GetPluralName(),
            name: name,
            dryRun: dryRun ? "All" : null
        );

        var patched = KubernetesJson.Deserialize<T>( response.Body.ToString() );

        return ( patched );
    }

    public static async Task<T> PatchNamespacedAsync<T>( this IKubernetes client
        , string ns
        , string name
        , T obj
        , bool dryRun = false )
    where T : IKubernetesObject<V1ObjectMeta>
    {
        var attr = typeof( T )
            .GetKubernetesEntityAttribute();

        var response = await client.CustomObjects.PatchNamespacedCustomObjectWithHttpMessagesAsync(
            body: new V1Patch( obj, V1Patch.PatchType.MergePatch ),
            group: attr.Group,
            version: attr.ApiVersion,
            namespaceParameter: ns,
            plural: attr.GetPluralName(),
            name: name,
            dryRun: dryRun ? "All" : null
        );

        var patched = KubernetesJson.Deserialize<T>( response.Body.ToString() );

        return ( patched );
    }

    public static async Task DeleteNamespacedAsync<T>( this IKubernetes client
        , string ns
        , string name
        , bool dryRun = false )
    where T : IKubernetesObject<V1ObjectMeta>
    {
        var attr = typeof( T )
            .GetKubernetesEntityAttribute();

        var response = await client.CustomObjects.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
            group: attr.Group,
            version: attr.ApiVersion,
            namespaceParameter: ns,
            plural: attr.GetPluralName(),
            name: name,
            dryRun: dryRun ? "All" : null
        );
    }
}
