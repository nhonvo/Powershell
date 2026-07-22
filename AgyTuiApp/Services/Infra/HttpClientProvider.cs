using System;
using System.Net.Http;

namespace AgyTui;

public static class HttpClientProvider
{
    public static readonly HttpClient Client = new();
}
