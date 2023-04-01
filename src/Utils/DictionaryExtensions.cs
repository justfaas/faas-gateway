internal static class DictionaryExtensions
{
    public static bool ContainsKeyWithValue<TValue>( this IDictionary<string, TValue>? source, string key, TValue wantedValue ) where TValue : notnull
    {
        if ( source == null )
        {
            return ( false );
        }

        if ( source.TryGetValue( key, out var value ) )
        {
            return value.Equals( wantedValue );
        }

        return ( false );
    }
}
