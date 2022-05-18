using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using System.Net;

namespace InternetArchive;

public static class ServiceExtensions
{
    private static IPolicyRegistry<string>? _pollyRegistry = null;
    internal static IServiceCollection? _services = null;

    public static IServiceCollection AddInternetArchiveServices(this IServiceCollection services, TimeSpan? timeout = null)
    {
        services.AddTransient<Client>();
        _pollyRegistry = services.AddPolicyRegistry();

        services.AddHttpClient(Name, client =>
        {
            client.Timeout = timeout ?? TimeSpan.FromMinutes(15);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = true,
#if NET
            AutomaticDecompression = DecompressionMethods.All,
#else
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
#endif
            UseCookies = false
        })
        .AddPolicyHandlerFromRegistry("RetryPolicy")
        .AddPolicyHandlerFromRegistry("ServiceUnavailablePolicy")
        .AddPolicyHandlerFromRegistry("TooManyRequestsPolicy");

        _services = services;
        return services;
    }

    public static IServiceCollection AddInternetArchiveDefaultRetryPolicies(this IServiceCollection services)
    {
        var logger = services.BuildServiceProvider().GetService<ILoggerFactory>()?.CreateLogger(Client.Name);

        var noRetryCodes = new HashSet<HttpStatusCode> { 
            HttpStatusCode.ServiceUnavailable,
            (HttpStatusCode) 429, // TooManyRequests
            HttpStatusCode.Unauthorized 
        };

        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && !noRetryCodes.Contains(r.StatusCode))
            .WaitAndRetryAsync(
                new[] { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8) }, 
                onRetry: (response, delay, retryAttempt, context) =>
                {
                    logger?.LogInformation("HTTP status {statusCode} retry #{retryAttempt} delay {delay}", (int) response.Result.StatusCode, retryAttempt, delay);
                });

        var serviceUnavailablePolicy = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(
                new[] { TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(120), TimeSpan.FromSeconds(180) },
                onRetry: (response, delay, retryAttempt, context) =>
                {
                    logger?.LogInformation("HTTP error {statusCode} retry #{retryAttempt} delay {delay}", (int)response.Result.StatusCode, retryAttempt, delay);
                });

        var tooManyRequestsPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == (HttpStatusCode) 429)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: (retryCount, response, context) => 
                {
                    var retryAfter = response?.Result?.Headers?.RetryAfter;
                    if (retryAfter == null) return TimeSpan.FromSeconds(60);

                    var duration = retryAfter?.Delta;
                    if (duration == null) return TimeSpan.FromSeconds(60);

                    if (duration > TimeSpan.FromMinutes(3)) duration = TimeSpan.FromMinutes(3);
                    return (TimeSpan) duration;
                },
                onRetryAsync: async (response, timespan, retryAttempt, context) => 
                {
                    await Task.FromResult(0);
                    logger?.LogInformation("HTTP error {statusCode} retry #{retryAttempt} delay {delay}", (int)response.Result.StatusCode, retryAttempt, timespan);
                });

        AddPolicy("RetryPolicy", retryPolicy);
        AddPolicy("ServiceUnavailablePolicy", serviceUnavailablePolicy);
        AddPolicy("TooManyRequestsPolicy", tooManyRequestsPolicy);

        _services = services;
        return services;
    }

    public static void AddPolicy(string name, IAsyncPolicy<HttpResponseMessage> policy)
    {
        if (_pollyRegistry == null) throw new Exception("Call AddInternetArchiveServices() first");
        _pollyRegistry.Add(name, policy);
    }
}