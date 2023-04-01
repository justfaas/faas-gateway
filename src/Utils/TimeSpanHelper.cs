using System.Text.RegularExpressions;

internal static class TimeSpanHelper
{
    private static List<(string abbrev, TimeSpan ts)> timeSpanDefault = new List<(string abbrev, TimeSpan ts)>
    {
        ( "h", TimeSpan.FromHours( 1 ) ),
        ( "m", TimeSpan.FromMinutes( 1 ) ),
        ( "s", TimeSpan.FromSeconds( 1 ) ),
    };

    public static TimeSpan? ParseDuration( string value )
    {
        if ( value == null )
        {
            return ( null );
        }

        try
        {
            return timeSpanDefault
                .Where( ts => value.Contains( ts.abbrev ) )
                .Select( ts => ts.ts * int.Parse( new Regex( @$"(\d+){ts.abbrev}" ).Match( value ).Groups[1].Value ) )
                .Aggregate( ( acc, ts ) => acc + ts );
        }
        catch ( Exception )
        {
            return ( null );
        }
    }
}
