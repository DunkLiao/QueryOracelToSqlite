using System.Text;

namespace OracleToSqlite.Core.Services;

public sealed class SqlFileReader : ISqlFileReader
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    static SqlFileReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<SqlFileReadResult> ReadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);

        if (HasUtf8Bom(bytes))
        {
            return new SqlFileReadResult(StrictUtf8.GetString(bytes[3..]), "UTF-8 BOM");
        }

        try
        {
            return new SqlFileReadResult(StrictUtf8.GetString(bytes), "UTF-8");
        }
        catch (DecoderFallbackException)
        {
            var cp950 = Encoding.GetEncoding(
                950,
                EncoderFallback.ExceptionFallback,
                DecoderFallback.ExceptionFallback);
            return new SqlFileReadResult(cp950.GetString(bytes), "CP950");
        }
    }

    private static bool HasUtf8Bom(IReadOnlyList<byte> bytes)
    {
        return bytes.Count >= 3 &&
               bytes[0] == 0xEF &&
               bytes[1] == 0xBB &&
               bytes[2] == 0xBF;
    }
}
