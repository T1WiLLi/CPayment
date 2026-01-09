namespace CPayment.Services;

internal static class HttpClientService
{
    private static readonly Lazy<HttpClient> _client = new(() =>
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd($"CPayment/{CPayment.Version}");
        return client;
    });

    public static HttpClient Instance => _client.Value;

}
