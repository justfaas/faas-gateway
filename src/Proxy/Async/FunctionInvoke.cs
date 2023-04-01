public sealed class FunctionInvoke
{
    public Dictionary<string, string> Arguments { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
    public IEnumerable<byte> Content { get; set; } = Enumerable.Empty<byte>();
    public string? ContentType { get; set; }
}
