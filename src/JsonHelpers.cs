using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace Community.System.Net.Http.Json;

internal static class JsonHelpers
{
    internal static MediaTypeHeaderValue GetDefaultMediaType()
    {
        return new("application/json") { CharSet = "utf-8" };
    }

    internal static Encoding GetEncoding(HttpContent content)
    {
        Encoding encoding = null;

        if (content.Headers.ContentType?.CharSet is string charset)
        {
            try
            {
                // Remove at most a single set of quotes.
                encoding = charset.Length > 2 && charset[0] == '\"' && charset[^1] == '\"'
                    ? Encoding.GetEncoding(charset[1..^1])
                    : Encoding.GetEncoding(charset);
            }
            catch (ArgumentException e)
            {
                throw new InvalidOperationException(SR.CharSetInvalid, e);
            }

            Debug.Assert(encoding != null);
        }

        return encoding;
    }
}
