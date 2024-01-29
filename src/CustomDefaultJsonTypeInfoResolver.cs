using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace Community.System.Net.Http.Json;

internal class CustomDefaultJsonTypeInfoResolver
{
    internal static DefaultJsonTypeInfoResolver DefaultInstance
    {
        [RequiresUnreferencedCode("SerializationUnreferencedCodeMessage")]
        [RequiresDynamicCode("SerializationDynamicCodeMessage")]
        get
        {
            if (s_defaultInstance is DefaultJsonTypeInfoResolver result)
            {
                return result;
            }

            DefaultJsonTypeInfoResolver newInstance = new();
            return Interlocked.CompareExchange(ref s_defaultInstance, newInstance, comparand: null) ?? newInstance;
        }
    }

    private static DefaultJsonTypeInfoResolver s_defaultInstance;
}
