using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

namespace Community.System.Net.Http.Json;

public static partial class HttpClientJsonExtensions
{
    /// <summary>
    /// Sends an HttpRequestMessage and returns the value that results
    /// from deserializing the response body as JSON in an async enumerable operation.
    /// </summary>
    /// <typeparam name="TValue">The target type to deserialize to.</typeparam>
    /// <param name="client">The client used to send the request.</param>
    /// <param name="request">The HttpRequestMessage used to send</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{TValue}"/> that represents the deserialized response body.</returns>
    /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
    [RequiresUnreferencedCode("SerializationUnreferencedCodeMessage")]
    [RequiresDynamicCode("SerializationDynamicCodeMessage")]
    public static IAsyncEnumerable<TValue> SendFromJsonAsAsyncEnumerable<TValue>(this HttpClient client, HttpRequestMessage request, CancellationToken cancellationToken = default)
        => SendFromJsonAsAsyncEnumerable<TValue>(client, request, options: null, cancellationToken);

    /// <summary>
    /// Sends an HttpRequestMessage and returns the value that results
    /// from deserializing the response body as JSON in an async enumerable operation.
    /// </summary>
    /// <typeparam name="TValue">The target type to deserialize to.</typeparam>
    /// <param name="client">The client used to send the request.</param>
    /// <param name="request">The HttpRequestMessage used to send</param>
    /// <param name="options"></param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{TValue}"/> that represents the deserialized response body.</returns>
    /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
    [RequiresUnreferencedCode("SerializationUnreferencedCodeMessage")]
    [RequiresDynamicCode("SerializationDynamicCodeMessage")]
    public static IAsyncEnumerable<TValue> SendFromJsonAsAsyncEnumerable<TValue>(this HttpClient client, HttpRequestMessage request, JsonSerializerOptions? options, CancellationToken cancellationToken = default) =>
        FromJsonStreamAsyncCore<TValue>(client, request, options, cancellationToken);

    [RequiresUnreferencedCode("SerializationUnreferencedCodeMessage")]
    [RequiresDynamicCode("SerializationDynamicCodeMessage")]
    private static IAsyncEnumerable<TValue> FromJsonStreamAsyncCore<TValue>(HttpClient client, HttpRequestMessage request, JsonSerializerOptions options, CancellationToken cancellationToken)
    {
        JsonTypeInfo<TValue> jsonTypeInfo = (JsonTypeInfo<TValue>)JsonHelpers.GetJsonTypeInfo(typeof(TValue), options);

        return FromJsonStreamAsyncCore(client, request, jsonTypeInfo, cancellationToken);
    }

    private static IAsyncEnumerable<TValue> FromJsonStreamAsyncCore<TValue>(HttpClient client, HttpRequestMessage request, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);

        return Core(client, request, jsonTypeInfo, cancellationToken);

        static async IAsyncEnumerable<TValue> Core(HttpClient client, HttpRequestMessage request, JsonTypeInfo<TValue> jsonTypeInfo, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using Stream readStream = await GetHttpResponseStreamAsync(client, response, false, cancellationToken).ConfigureAwait(false);

            await foreach (TValue value in JsonSerializer.DeserializeAsyncEnumerable<TValue>(readStream, jsonTypeInfo, cancellationToken).ConfigureAwait(false))
            {
                yield return value;
            }
        }
    }

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
