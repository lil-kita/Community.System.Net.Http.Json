# Community.System.Net.Http.Json
## Installation

Install package through NuGet - [Community.System.Net.Http.Json](https://www.nuget.org/packages/Community.System.Net.Http.Json/)  

## Usage

The library provides `SendFromJsonAsAsyncEnumerable` extension methods:
1) `IAsyncEnumerable<TValue> SendFromJsonAsAsyncEnumerable<TValue>(this HttpClient client, HttpRequestMessage request, CancellationToken cancellationToken = default)`
2) `IAsyncEnumerable<TValue> SendFromJsonAsAsyncEnumerable<TValue>(this HttpClient client, HttpRequestMessage request, JsonSerializerOptions options, CancellationToken cancellationToken = default)`
