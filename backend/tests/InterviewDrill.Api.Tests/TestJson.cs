using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InterviewDrill.Api.Tests;

public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
    };

    public static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string uri, T value) =>
        client.PostAsJsonAsync(uri, value, Options);

    public static async Task<T?> ReadJsonAsync<T>(this HttpContent content)
    {
        return await content.ReadFromJsonAsync<T>(Options);
    }
}
