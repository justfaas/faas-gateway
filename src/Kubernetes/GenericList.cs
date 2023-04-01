using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

public sealed class GenericList<T> : KubernetesObject
    , IKubernetesObject<V1ListMeta>
    , IItems<T>
where T : IKubernetesObject<V1ObjectMeta>
{
    [JsonPropertyName( "metadata" )]
    public V1ListMeta Metadata { get; set; } = new V1ListMeta();

    [JsonPropertyName( "items" )]
    public IList<T> Items { get; set; } = new List<T>();
}
