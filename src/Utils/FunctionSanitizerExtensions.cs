using Faactory.k8s.Models;
using k8s.Models;

internal static class FunctionSanitizerExtensions
{
    public static object Minimal( this V1Alpha1Function func )
        => new
        {
            ApiVersion = func.ApiVersion,
            Kind = func.Kind,
            Metadata = new
            {
                Name = func.Name(),
                Namespace = func.Namespace()
            },
            Spec = new
            {
                Image = func.Spec.Image,
                Port = func.Spec.Port
            }
        };
}
