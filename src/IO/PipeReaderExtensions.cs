using System.IO.Pipelines;

internal static class PipeReaderExtensions
{
    public static async Task<byte[]> ReadAsByteArrayAsync( this PipeReader reader )
    {
        using ( var ms = new MemoryStream() )
        {
            await reader.CopyToAsync( ms );

            return ms.ToArray();
        }
    }
}
