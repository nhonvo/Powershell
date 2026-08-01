using System.Net.Http;

namespace AgyTui.Infrastructure.Common;

public class HttpClientProvider : IHttpClientProvider
{
    private static readonly Lazy<HttpClientProvider> _instance = new(() => new HttpClientProvider());
    public static HttpClientProvider Instance => _instance.Value;

    public HttpClient Client { get; } = new();

    public static HttpClient StaticClient => Instance.Client;
    public static HttpClient ClientInstance => Instance.Client;
}
