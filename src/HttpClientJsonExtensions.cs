using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;

namespace Community.System.Net.Http.Json;

public static partial class HttpClientJsonExtensions
{
    private static ValueTask<Stream> GetHttpResponseStreamAsync(HttpClient client, HttpResponseMessage response, bool usingResponseHeadersRead, CancellationToken cancellationToken)
    {
        Debug.Assert(client.MaxResponseContentBufferSize is > 0 and <= int.MaxValue);
        int contentLengthLimit = (int)client.MaxResponseContentBufferSize;

        if (response.Content.Headers.ContentLength is long contentLength && contentLength > contentLengthLimit)
        {
            LengthLimitReadStream.ThrowExceededBufferLimit(contentLengthLimit);
        }

        ValueTask<Stream> task = GetContentStreamAsync(response.Content, cancellationToken);

        // If ResponseHeadersRead wasn't used, HttpClient will have already buffered the whole response upfront.
        // No need to check the limit again.
        return usingResponseHeadersRead ? GetLengthLimitReadStreamAsync(client, task) : task;
    }

    private static async ValueTask<Stream> GetLengthLimitReadStreamAsync(HttpClient client, ValueTask<Stream> task)
    {
        Stream contentStream = await task.ConfigureAwait(false);
        return new LengthLimitReadStream(contentStream, (int)client.MaxResponseContentBufferSize);
    }

    private static ValueTask<Stream> GetContentStreamAsync(HttpContent content, CancellationToken cancellationToken)
    {
        Task<Stream> task = ReadHttpContentStreamAsync(content, cancellationToken);

        return JsonHelpers.GetEncoding(content) is Encoding sourceEncoding && sourceEncoding != Encoding.UTF8
            ? GetTranscodingStreamAsync(task, sourceEncoding)
            : new(task);
    }

    private static async ValueTask<Stream> GetTranscodingStreamAsync(Task<Stream> task, Encoding sourceEncoding)
    {
        Stream contentStream = await task.ConfigureAwait(false);

        // Wrap content stream into a transcoding stream that buffers the data transcoded from the sourceEncoding to utf-8.
        return GetTranscodingStream(contentStream, sourceEncoding);
    }

    private static Task<Stream> ReadHttpContentStreamAsync(HttpContent content, CancellationToken cancellationToken)
    {
        return content.ReadAsStreamAsync(cancellationToken);
    }

    private static Stream GetTranscodingStream(Stream contentStream, Encoding sourceEncoding)
    {
        return Encoding.CreateTranscodingStream(contentStream, innerStreamEncoding: sourceEncoding, outerStreamEncoding: Encoding.UTF8);
    }
}
