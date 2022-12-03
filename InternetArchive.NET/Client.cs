global using Microsoft.AspNetCore.JsonPatch;
global using Microsoft.AspNetCore.WebUtilities;
global using Microsoft.Extensions.Logging;
global using System.Collections.Concurrent;
global using System.Security.Cryptography;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;

global using static InternetArchive.Client;
global using static System.Web.HttpUtility;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace InternetArchive;

public class Client
{
    public const string Name = "InternetArchive.NET";
    internal static JsonSerializerOptions _jsonSerializerOptions = null!;

    internal readonly ILogger _logger;
    public Client(ILogger? logger = null, ILoggerFactory? loggerFactory = null, HttpClient? httpClient = null, IHttpClientFactory? httpClientFactory = null)
    {
        _logger = logger ?? loggerFactory?.CreateLogger(Name) ?? NullLogger.Instance;
        HttpClient =  httpClientFactory?.CreateClient(Name) ?? httpClient ?? throw new Exception("Must pass an HttpClient or an HttpClientFactory");

        _jsonSerializerOptions ??= new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        Changes = new Changes(this);
        Item = new Item(this);
        Metadata = new Metadata(this);
        Relationships = new Relationships(this);
        Reviews = new Reviews(this);
        Search = new Search(this);
        Tasks = new Tasks(this);
        Views = new Views(this);
        Wayback = new Wayback(this);
    }

    private HttpClient HttpClient { get; set; }
    private void InitHttpClient()
    {
        if (!ReadOnly && !DryRun)
        {
            HttpClient.DefaultRequestHeaders.Add("authorization", $"LOW {AccessKey}:{SecretKey}");
        }

        string? version = Assembly.GetExecutingAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (version == null) throw new Exception("Unable to get version");

        var productValue = new ProductInfoHeaderValue(Name, version);
        var commentValue = new ProductInfoHeaderValue("(+https://github.com/experimentaltvcenter/InternetArchive.NET)");

        HttpClient.DefaultRequestHeaders.UserAgent.Add(productValue);
        HttpClient.DefaultRequestHeaders.UserAgent.Add(commentValue);
        HttpClient.DefaultRequestHeaders.ExpectContinue = true;
    }

    public bool ReadOnly { get; private set; }
    public bool DryRun { get; private set; }

    internal string AccessKey { get; set; } = null!;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal string SecretKey { get; set; } = null!;

    public Changes Changes { get; private set; }
    public Item Item { get; private set; }
    public Metadata Metadata { get; private set; }
    public Relationships Relationships { get; private set; }
    public Reviews Reviews { get; private set; }
    public Search Search { get; private set; }
    public Tasks Tasks { get; private set; }
    public Views Views { get; private set; }
    public Wayback Wayback { get; private set; }

    public static Client CreateReadOnly(bool dryRun = false)
    {
        var client = GetClient(readOnly: true, dryRun);
        client.InitHttpClient();
        return client;
    }

    public static Client Create(string accessKey, string secretKey, bool readOnly = false, bool dryRun = false)
    {
        var client = GetClient(readOnly, dryRun);

        client.AccessKey = accessKey;
        client.SecretKey = secretKey;

        client.InitHttpClient();
        return client;
    }

    public static async Task<Client> CreateAsync(string? emailAddress = null, string? password = null, bool readOnly = false, bool dryRun = false)
    {
        var client = GetClient(readOnly, dryRun);

        if (emailAddress == null || password == null)
        {
            var loginPrompt = new StringBuilder("Log in to archive.org");

            var restrictions = new List<string>();
            if (readOnly) restrictions.Add("readOnly");
            if (dryRun) restrictions.Add("dryRun");
            if (restrictions.Any()) loginPrompt.Append($" [{string.Join(", ", restrictions)}]");

            Console.WriteLine(loginPrompt);
            Console.WriteLine();

            if (!readOnly)
            {
                string message = "Email address";
                if (string.IsNullOrWhiteSpace(emailAddress))
                {
                    Console.Write($"{message}: ");
                    emailAddress = Console.ReadLine();
                }
                else
                {
                    Console.WriteLine($"{message}: {emailAddress}");
                }

                if (string.IsNullOrWhiteSpace(emailAddress)) throw new Exception("Email address required");

                password ??= ReadPasswordFromConsole("Password: ");
            }
        }

        if (!readOnly)
        {
            await client.LoginAsync(emailAddress!, password!, readOnly).ConfigureAwait(false);
        }

        client.InitHttpClient();
        return client;
    }

    private static Client GetClient(bool readOnly, bool dryRun)
    {
        var client = ServiceExtensions.Services.InitDefaults().BuildServiceProvider().GetRequiredService<Client>();
        
        client.ReadOnly = readOnly;
        client.DryRun = dryRun;

        return client;
    }

    public void RequestInteractivePriority()
    {
        HttpClient.DefaultRequestHeaders.Add("x-archive-interactive-priority", "1");
    }

    internal async Task<Response> GetAsync<Response>(string url, Dictionary<string, string>? query = null)
    {
        if (query != null) url = QueryHelpers.AddQueryString(url, query);

        using var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(url)
        };

        var response = await SendAsync<Response>(httpRequest).ConfigureAwait(false);
        if (response == null) throw new Exception("null response from server");
        return response;
    }

    internal static HashSet<HttpMethod> _readOnlyMethods = new() { HttpMethod.Head, HttpMethod.Get };   
    internal async Task<Response?> SendAsync<Response>(HttpRequestMessage request)
    {
        Log(request);
        if (request.RequestUri?.Scheme == "http" && ReadOnly == false) throw new Exception("Insecure call");

        if (ReadOnly && !_readOnlyMethods.Contains(request.Method))
        {
            if (DryRun)
            {
                _logger.LogInformation("dry run");
                return default;
            }
            else
            {
                throw new InvalidOperationException("Cannot call this function when the client is configured in read-only mode");
            }
        }

        var httpResponse = await HttpClient.SendAsync(request).ConfigureAwait(false);
        Log(httpResponse);
        httpResponse.EnsureSuccessStatusCode();

        if (typeof(Response) == typeof(HttpResponseMessage))
        {
            return (Response)(object)httpResponse;
        }

        var responseString = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (typeof(Response) == typeof(string))
        {
            return (Response)(object)responseString;
        }

        if (httpResponse?.Content?.Headers?.ContentType?.MediaType == "application/xml")
        {
            var serializer = new XmlSerializer(typeof(Response));
            var xmlReader = XmlReader.Create(new StringReader(responseString));
            return (Response?) serializer.Deserialize(xmlReader);
        }
        else
        {
            return JsonSerializer.Deserialize<Response>(responseString, _jsonSerializerOptions);
        }
    }

    internal async Task<Response?> SendAsync<Response>(HttpMethod httpMethod, string url, object content)
    {
        var json = JsonSerializer.Serialize(content, _jsonSerializerOptions);
        var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage
        {
            RequestUri = new Uri(url),
            Method = httpMethod,
            Content = stringContent
        };

        return await SendAsync<Response>(httpRequest).ConfigureAwait(false);
    }

    internal async Task<Response?> SendAsync<Response>(HttpMethod httpMethod, string url, HttpContent content)
    {
        using var httpRequest = new HttpRequestMessage
        {
            RequestUri = new Uri(url),
            Method = httpMethod,
            Content = content
        };

        return await SendAsync<Response>(httpRequest).ConfigureAwait(false);
    }

    private void Log(HttpRequestMessage request)
    {
        var logLevel = LogLevel.Information;
        if (!_logger.IsEnabled(logLevel)) return;

        _logger.Log(logLevel, "{method} {url}", request.Method, request.RequestUri);

        if (request.Headers.Any())
        {
            using (_logger.BeginScope("Request headers:"))
            {
                foreach (var kvp in request.Headers)
                {
                    foreach (var value in kvp.Value)
                    {
                        _logger.Log(logLevel, "{key}: {value}", kvp.Key, value);
                    }
                }
            }
        }
    }

    private HttpResponseMessage Log(HttpResponseMessage response)
    {
        var logLevel = LogLevel.Information;
        if (_logger.IsEnabled(logLevel))
        {
            if (response.Headers.Any())
            {
                using (_logger.BeginScope("Response headers:"))
                {
                    foreach (var kvp in response.Headers)
                    {
                        foreach (var value in kvp.Value)
                        {
                            _logger.Log(logLevel, "{key}: {value}", kvp.Key, value);
                        }
                    }
                }
            }

            string body = response.Content.ReadAsStringAsync().Result;
            _logger.Log(logLevel, "Response body: {body}", body);
            _logger.Log(response.IsSuccessStatusCode ? logLevel : LogLevel.Error, "Result: {StatusCode} {ReasonPhrase}", (int)response.StatusCode, response.ReasonPhrase);
        }

        return response;
    }

    private class LoginResponse : ServerResponse
    {
        public int Version { get; set; }

        public Values_? Values { get; set; }

        internal class Values_
        {
            public string? Reason { get; set; }

            internal class Cookies_
            {
                [JsonPropertyName("logged-in-sig")]
                public string? LoggedInSig { get; set; }

                [JsonPropertyName("logged-in-user")]
                public string? LoggedInUser { get; set; }
            }

            public Cookies_? Cookies { get; set; }

            public string? Email { get; set; }
            public string? ItemName { get; set; }

            internal class S3_
            {
                [JsonPropertyName("access")]
                public string AccessKey { get; set; } = null!;

                [JsonPropertyName("secret")]
                public string SecretKey { get; set; } = null!;
            }

            public S3_? S3 { get; set; }

            public string? ScreenName { get; set; }
        }
    }

    private async Task LoginAsync(string emailAddress, string password, bool readOnly)
    {
        string url = "https://archive.org/services/xauthn/?op=login";

        var formData = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("email", emailAddress),
            new KeyValuePair<string, string>("password", password)
        };

        var httpContent = new FormUrlEncodedContent(formData);

        _logger.LogInformation("Logging in...");
        var httpResponse = await HttpClient.PostAsync(url, httpContent).ConfigureAwait(false);
        Log(httpResponse);

        if (httpResponse == null) throw new NullReferenceException("httpResponse");

        var json = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(json, _jsonSerializerOptions);

        if (httpResponse.IsSuccessStatusCode)
        {
            if (loginResponse == null || loginResponse.Values == null || loginResponse.Values.S3 == null) throw new NullReferenceException("loginResponse");
            loginResponse.EnsureSuccess();

            if (readOnly == false)
            {
                AccessKey = loginResponse.Values.S3.AccessKey;
                SecretKey = loginResponse.Values.S3.SecretKey;
            }
        }
        else if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new Exception($"Login failed: {loginResponse?.Values?.Reason}");
        }

        httpResponse.EnsureSuccessStatusCode();
    }

    private static string ReadPasswordFromConsole(string prompt)
    {
        Console.Write(prompt);

        string password = "";

        while (true)
        {
            var keyInfo = Console.ReadKey(true);

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }
            else if (keyInfo.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password.Substring(0, password.Length - 1);
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                password += keyInfo.KeyChar;
            }
        }

        return password;
    }
}
