using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json;

namespace Community.System.Net.Http.Json;

internal static class JsonHelpers
{
    [RequiresUnreferencedCode("SerializationUnreferencedCodeMessage")]
    [RequiresDynamicCode("SerializationDynamicCodeMessage")]
    internal static JsonTypeInfo GetJsonTypeInfo(Type type, JsonSerializerOptions options)
    {
        Debug.Assert(type is not null);

        // Resolves JsonTypeInfo metadata using the appropriate JsonSerializerOptions configuration,
        // following the semantics of the JsonSerializer reflection methods.
        options ??= CustomJsonSerializerOptions.Web;
        options.MakeReadOnly(populateMissingResolver: true);
        return options.GetTypeInfo(type);
    }

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
