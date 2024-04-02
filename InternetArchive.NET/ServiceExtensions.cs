using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace InternetArchive;

public static class ServiceExtensions
{
    public static IServiceCollection Services { get; private set; } = new ServiceCollection();
    private static readonly List<IAsyncPolicy<HttpResponseMessage>> Policies = [];
    private static ILogger? Logger;

    internal static IServiceCollection InitDefaults(this IServiceCollection services)
    {
        if (!services.Any(x => x.ServiceType == typeof(Client))) 
        {
            Logger = services.BuildServiceProvider().GetService<ILoggerFactory>()?.CreateLogger(Name);
            services.AddInternetArchiveDefaultRetryPolicies().AddInternetArchiveServices();
        }

        return services;
    }

    public static IServiceCollection AddInternetArchiveServices(this IServiceCollection services, TimeSpan? timeout = null)
    {
        services.AddTransient<Client>();

        var clientBuilder = services.AddHttpClient(Name, client =>
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
        });

        foreach (var policy in Policies)
        {
            clientBuilder.AddPolicyHandler(policy);
        }

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
                [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8)], 
                onRetry: (response, delay, retryAttempt, context) =>
                {
                    Logger?.LogInformation("HTTP status {statusCode} retry #{retryAttempt} delay {delay}", (int) response.Result.StatusCode, retryAttempt, delay);
                });

        var retryPutPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.NotFound && r.RequestMessage?.Method == HttpMethod.Put)
            .WaitAndRetryAsync(
                [TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(60)],
                onRetry: (response, delay, retryAttempt, context) =>
                {
                    Logger?.LogInformation("HTTP PUT status {statusCode} retry #{retryAttempt} delay {delay}", (int)response.Result.StatusCode, retryAttempt, delay);
                });

        var serviceUnavailablePolicy = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(
                [TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(120), TimeSpan.FromSeconds(180)],
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
            .AddPolicy(retryPolicy)
            .AddPolicy(retryPutPolicy)
            .AddPolicy(serviceUnavailablePolicy)
            .AddPolicy(tooManyRequestsPolicy);

        return services;
    }

    public static IServiceCollection AddPolicy(this IServiceCollection services, IAsyncPolicy<HttpResponseMessage> policy)
    {
        Policies.Add(policy);
        return services;
    }

    [Obsolete("Please remove the name parameter and use AddPolicy(policy) directly")]
    public static IServiceCollection AddPolicy(this IServiceCollection services, string _, IAsyncPolicy<HttpResponseMessage> policy)
    {
        Policies.Add(policy);
        return services;
    }
}
