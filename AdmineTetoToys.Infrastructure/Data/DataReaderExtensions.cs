using System.Data.Common;

namespace AdmineTetoToys.Infrastructure.Data;

internal static class DataReaderExtensions
{
    /// <summary>
    /// Reads a CHAR(36) id column as a string.
    ///
    /// MySql.Data materialises CHAR(36) as <see cref="Guid"/> by default, so calling
    /// GetString on such a column throws InvalidCastException — but the same column
    /// comes back as a plain string when the value isn't GUID-shaped or when the
    /// connection uses OldGuids. Reading the boxed value handles both, so callers
    /// don't have to know which representation a given column happens to use.
    ///
    /// Declared on DbDataReader, not MySqlDataReader: ExecuteReaderAsync returns the
    /// base type, so an extension on the concrete type would never bind.
    /// </summary>
    public static string GetIdString(this DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal)) return string.Empty;

        var value = reader.GetValue(ordinal);
        return value as string ?? value?.ToString() ?? string.Empty;
    }
}
