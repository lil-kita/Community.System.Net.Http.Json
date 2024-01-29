using System.Globalization;

namespace Community.System.Net.Http.Json;

internal static partial class SR
{
    // The resource generator used in AspNetCore does not create this method. This file fills in that functional gap
    // so we don't have to modify the shared source.
    internal static string Format(string resourceFormat, params object[] args)
    {
        return args != null ? string.Format(CultureInfo.CurrentCulture, resourceFormat, args) : resourceFormat;
    }

    internal static string net_http_content_buffersize_exceeded = "Cannot write more bytes to the buffer than the configured maximum buffer size: {0}.";

    internal static string CharSetInvalid = "The character set provided in ContentType is invalid.";
}
