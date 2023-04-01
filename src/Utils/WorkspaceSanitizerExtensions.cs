using Faactory.k8s.Models;
using k8s.Models;

internal static class WorkspaceSanitizerExtensions
{
    public static object Minimal( this V1Alpha1Workspace ws )
        => new
        {
            ApiVersion = ws.ApiVersion,
            Kind = ws.Kind,
            Metadata = new
            {
                Name = ws.Name(),
                Namespace = ws.Namespace()
            }
        };
}
