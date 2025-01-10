global using InternetArchive;
global using Microsoft.AspNetCore.JsonPatch;
global using Microsoft.VisualStudio.TestTools.UnitTesting;
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net;
global using System.Text.Json;
global using System.Threading.Tasks;
global using static InternetArchiveTests.Init;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

[assembly: Parallelize(Workers = 8, Scope = ExecutionScope.MethodLevel)]

namespace InternetArchiveTests;

[TestClass()]
public static class Init
{
    internal static Client _client = null!;
    internal static Config _config = null!;

    internal static DateOnly _startDateOnly, _endDateOnly;
    internal static DateTime _startDateTime, _endDateTime;

    internal static HttpClient _httpClient = new();
    internal static Random _random = new();

    [AssemblyInitialize]
    public static void TestInitialize(TestContext _)
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.private.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        _config = configurationBuilder.Get<Config>() ?? throw new Exception("_config is null");

        if (string.IsNullOrEmpty(_config.AccessKey))
        {
            throw new Exception("To run tests, please create a private settings file or set environment variables. For details visit https://github.com/experimentaltvcenter/InternetArchive.NET/blob/main/docs/DEVELOPERS.md#unit-tests");
        }

        ServiceExtensions.Services.AddLogging(configure => configure.AddConsole(options => options.FormatterName = ConsoleFormatterNames.Systemd));

        _client = Client.Create(_config.AccessKey, _config.SecretKey);
        _client.RequestInteractivePriority();

        _endDateTime = DateTime.Today.AddDays(-1);
        _startDateTime = _endDateTime.AddDays(-7);

        _endDateOnly = new DateOnly(_endDateTime.Year, _endDateTime.Month, _endDateTime.Day);
        _startDateOnly = new DateOnly(_startDateTime.Year, _startDateTime.Month, _startDateTime.Day);

        if (!File.Exists(_config.LocalFilename)) File.WriteAllText(_config.LocalFilename, "test file for unit tests - ok to delete");
    }

    private static string _sharedTestIdentifier = null!;
    private static readonly SemaphoreSlim _semaphore = new(1);

    internal static async Task<string> GetSharedTestIdentifierAsync()
    {
        if (_sharedTestIdentifier != null) return _sharedTestIdentifier;

        await _semaphore.WaitAsync();

        try
        {
            return _sharedTestIdentifier = await CreateTestItemAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    internal static string GenerateIdentifier()
    {
        return $"tmp-{Guid.NewGuid():N}";
    }

    internal static void DisplayStatus(Item.PutRequest.UploadStatus status)
    {
        var sb = new StringBuilder(status.Request.Bucket);

        var remoteName = status.Request.RemoteFilename ?? Path.GetFileName(status.Request.LocalPath);
        if (remoteName != null) sb.Append($"/{remoteName}");
        sb.Append(": ");
        
        if (status.Part.HasValue) sb.Append($"part {status.Part} of {status.TotalParts} | ");

        sb.Append($"{status.PercentComplete:P0} | {status.BytesUploaded} of {status.TotalBytes} bytes");

        Console.WriteLine(sb);
    }

    internal static async Task<string> CreateTestItemAsync(string? identifier = null, IEnumerable<KeyValuePair<string, object?>>? extraMetadata = null)
    {
        identifier ??= GenerateIdentifier();

        var metadata = new List<KeyValuePair<string, object?>>
        {
            new("collection", "test_collection"),
            new("mediatype", "texts"),
            new("noindex", "true"),
        };

        if (extraMetadata != null) metadata.AddRange(extraMetadata);

        var putRequest = new Item.PutRequest
        {
            Bucket = identifier,
            LocalPath = _config.LocalFilename,
            RemoteFilename = _config.RemoteFilename,
            Metadata = metadata,
            CreateBucket = true,
            NoDerive = true
        };

        putRequest.ProgressChanged += DisplayStatus;

        await _client.Item.PutAsync(putRequest);
        await WaitForServerAsync(identifier);
        await VerifyHashesAsync(putRequest);

        return identifier;
    }

    internal static async Task VerifyHashesAsync(Item.PutRequest request)
    {
        Assert.IsNotNull(request.Bucket);

        var sourceStream = request.SourceStream ?? File.OpenRead(request.LocalPath!);

        try
        {
            var md5 = MD5.Create().ComputeHash(sourceStream);
            sourceStream.Seek(0, SeekOrigin.Begin);
            var sha1 = SHA1.Create().ComputeHash(sourceStream);
            sourceStream.Seek(0, SeekOrigin.Begin);

            var metadata = await _client.Metadata.ReadAsync(request.Bucket);

            Assert.IsNotNull(metadata);
            Assert.IsTrue(metadata.Files.Any());
            var file = metadata.Files.Where(x => x.Name == request.Filename(encoded: false)).SingleOrDefault();
            Assert.IsNotNull(file);

            Assert.AreEqual(Convert.ToHexString(md5).ToLowerInvariant(), file.Md5, "MD5 does not match");
            Assert.AreEqual(Convert.ToHexString(sha1).ToLowerInvariant(), file.Sha1, "SHA1 does not match");
        }
        finally
        {
            if (request.SourceStream == null) sourceStream?.Dispose();
        }
    }

    public static async Task WaitForServerAsync(string identifier, int minutes = 30, int secondsBetween = 10)   
    {
        int retries = minutes * 60 / secondsBetween;

        for (int i = 0; i < retries; i++)
        {
            var taskRequest = new Tasks.GetRequest { Identifier = identifier };
            var response = await _client.Tasks.GetAsync(taskRequest);
            Assert.IsTrue(response.Success);

            var summary = response.Value!.Summary!;
            Assert.AreEqual(0, summary.Error);

            if (summary.Queued == 0 && summary.Running == 0) return;
            await Task.Delay(secondsBetween * 1000);
        }

        Assert.Fail($"timeout of {minutes} minutes exceeded");
    }
}
