internal sealed class ExportedMetric
{
    public string Metric { get; set; } = string.Empty;
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    public ulong Value { get; set; }
}

/*
# HELP faas_proxy_requests_total Number of requests over the proxy.
# TYPE faas_proxy_requests_total counter
faas_proxy_requests_total{namespace="faas",function="gateway"} 1
*/

internal static class PrometheusParser
{
    public static IEnumerable<ExportedMetric> Parse( string content )
    {
        var items = content.Split( '\n', StringSplitOptions.RemoveEmptyEntries & StringSplitOptions.TrimEntries )
            .Where( x => !string.IsNullOrWhiteSpace( x ) )
            .Where( x => !x.StartsWith( "#" ) );

        var metrics = new List<ExportedMetric>();

        foreach ( var item in items )
        {
            var pair = item.Split( ' ', StringSplitOptions.TrimEntries );

            var key = pair[0];
            var value = UInt64.Parse( pair[1] );

            var labelIdx = key.IndexOf( '{' );
            var closureIdx = key.IndexOf( '}' );

            if ( closureIdx < labelIdx )
            {
                throw new ArgumentException( "Invalid label format!" );
            }

            var metricName = key.Substring( 0, labelIdx );

            var labels = key.Substring( labelIdx + 1, closureIdx - labelIdx - 1 )
                .Split( ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries )
                .Select( x =>
                {
                    var pair = x.Split( '=' );

                    return new KeyValuePair<string, string>( pair[0], pair[1].Trim( '"' ) );
                } );

            metrics.Add( new ExportedMetric
            {
                Metric = metricName,
                Labels = new Dictionary<string, string>( labels ),
                Value = value
            } );
        }

        return ( metrics );
    }
}
