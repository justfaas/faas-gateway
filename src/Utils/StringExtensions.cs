internal static class StringExtensions
{
    public static string EnsureEndsWith( this string source, string value )
    {
        if ( source.EndsWith( value ) )
        {
            return ( source );
        }

        return string.Concat( source, value );
    }

    public static int ToInt32( this string source, int defaultValue = default( int ) )
    {
        if ( !Int32.TryParse( source, out var value ) )
        {
            return ( defaultValue );
        }

        return ( value );
    }
}
