namespace AgyTui.Infrastructure.Common;

public interface IHttpClientProvider
{
    HttpClient Client { get; }
}
