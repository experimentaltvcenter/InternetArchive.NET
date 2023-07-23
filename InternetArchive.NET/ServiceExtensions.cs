using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;

namespace InternetArchive;

public static class ServiceExtensions
{
    public static IServiceCollection Services { get; private set; } = new ServiceCollection();
    private static readonly Lazy<IPolicyRegistry<string>> PolicyRegistry = new(() => Services.AddPolicyRegistry());
    private static ILogger? Logger;

    internal static IServiceCollection InitDefaults(this IServiceCollection services)
    {
        if (!services.Any(x => x.ServiceType == typeof(Client))) 
        {
            Logger = services.BuildServiceProvider().GetService<ILoggerFactory>()?.CreateLogger(Name);
            services.AddInternetArchiveServices().AddInternetArchiveDefaultRetryPolicies();
        }

        return services;
    }

    public static IServiceCollection AddInternetArchiveServices(this IServiceCollection services, TimeSpan? timeout = null)
    {
        services.AddTransient<Client>();

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
        .AddPolicyHandlerFromRegistry("RetryPutPolicy")
        .AddPolicyHandlerFromRegistry("ServiceUnavailablePolicy")
        .AddPolicyHandlerFromRegistry("TooManyRequestsPolicy");

        return services;
    }

    public static IServiceCollection AddInternetArchiveDefaultRetryPolicies(this IServiceCollection services)
    {
        var HttpStatusCode_TooManyRequests = (HttpStatusCode)429;

        var noRetryCodes = new HashSet<HttpStatusCode> {
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode_TooManyRequests,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.BadRequest,
            HttpStatusCode.NotFound
        };

        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && !noRetryCodes.Contains(r.StatusCode))
            .WaitAndRetryAsync(
                new[] { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8) }, 
                onRetry: (response, delay, retryAttempt, context) =>
                {
                    Logger?.LogInformation("HTTP status {statusCode} retry #{retryAttempt} delay {delay}", (int) response.Result.StatusCode, retryAttempt, delay);
                });

        var retryPutPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.NotFound && r.RequestMessage?.Method == HttpMethod.Put)
            .WaitAndRetryAsync(
                new[] { TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(60) },
                onRetry: (response, delay, retryAttempt, context) =>
                {
                    Logger?.LogInformation("HTTP PUT status {statusCode} retry #{retryAttempt} delay {delay}", (int)response.Result.StatusCode, retryAttempt, delay);
                });

        var serviceUnavailablePolicy = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(
                new[] { TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(120), TimeSpan.FromSeconds(180) },
                onRetry: (response, delay, retryAttempt, context) =>
                {
                    Logger?.LogInformation("HTTP error {statusCode} retry #{retryAttempt} delay {delay}", (int)response.Result.StatusCode, retryAttempt, delay);
                });

        var tooManyRequestsPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode_TooManyRequests)
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
                    await Task.FromResult(0).ConfigureAwait(false);
                    Logger?.LogInformation("HTTP error {statusCode} retry #{retryAttempt} delay {delay}", (int)response.Result.StatusCode, retryAttempt, timespan);
                });

        services
            .AddPolicy("RetryPolicy", retryPolicy)
            .AddPolicy("RetryPutPolicy", retryPutPolicy)
            .AddPolicy("ServiceUnavailablePolicy", serviceUnavailablePolicy)
            .AddPolicy("TooManyRequestsPolicy", tooManyRequestsPolicy);

        return services;
    }

    public static IServiceCollection AddPolicy(this IServiceCollection services, string name, IAsyncPolicy<HttpResponseMessage> policy)
    {
        PolicyRegistry.Value.Add(name, policy);
        return services;
    }
}
