using Community.System.Net.Http.Json.src;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Community.System.Net.Http.Json;

public sealed partial class CustomJsonSerializerOptions
{
    /// <summary>
    /// Gets a read-only, singleton instance of <see cref="JsonSerializerOptions" /> that uses the web configuration.
    /// </summary>
    /// <remarks>
    /// Each <see cref="JsonSerializerOptions" /> instance encapsulates its own serialization metadata caches,
    /// so using fresh default instances every time one is needed can result in redundant recomputation of converters.
    /// This property provides a shared instance that can be consumed by any number of components without necessitating any converter recomputation.
    /// </remarks>
    public static JsonSerializerOptions Web
    {
        [RequiresUnreferencedCode("SerializationUnreferencedCodeMessage")]
        [RequiresDynamicCode("SerializationDynamicCodeMessage")]
        get => s_webOptions ?? GetOrCreateSingleton(ref s_webOptions, JsonSerializerDefaults.Web);
    }

    private static JsonSerializerOptions s_webOptions;

    [RequiresUnreferencedCode("SerializationUnreferencedCodeMessage")]
    [RequiresDynamicCode("SerializationDynamicCodeMessage")]
    private static JsonSerializerOptions GetOrCreateSingleton(ref JsonSerializerOptions location, JsonSerializerDefaults defaults)
    {
        JsonSerializerOptions options = new(defaults)
        {
            // Because we're marking the default instance as read-only,
            // we need to specify a resolver instance for the case where
            // reflection is disabled by default: use one that returns null for all types.

            TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault
                ? CustomDefaultJsonTypeInfoResolver.DefaultInstance
                : CustomJsonTypeInfoResolver.Empty,
        };

        return Interlocked.CompareExchange(ref location, options, null) ?? options;
    }
}
