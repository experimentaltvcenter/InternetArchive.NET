namespace InternetArchive;

public class Metadata
{
    internal static string Url(string identifier) => $"https://archive.org/metadata/{identifier}";

    private readonly Client _client;
    public Metadata(Client client)
    {
        _client = client;
    }

    public class ReadResponse : IDisposable
    {
        [JsonPropertyName("created")]
        [JsonConverter(typeof(UnixEpochDateTimeNullableConverter))]
        public DateTimeOffset? DateCreated { get; set; }

        [JsonPropertyName("d1")]
        public string DataNodePrimary { get; set; } = null!;

        [JsonPropertyName("d2")]
        public string? DataNodeSecondary { get; set; }

        [JsonPropertyName("solo")]
        public bool? DataNodeSolo { get; set; }

        public string Dir { get; set; } = null!;

        public IEnumerable<File> Files { get; set; } = Enumerable.Empty<File>();

        public class File
        {
            public string? Name { get; set; }
            public string? Source { get; set; }
            public string? Original { get; set; }

            [JsonPropertyName("mtime")]
            [JsonConverter(typeof(UnixEpochDateTimeNullableConverter))]
            public DateTimeOffset? ModificationDate { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
            public long? Size { get; set; }

            public string? Md5 { get; set; }
            public string? Crc32 { get; set; }
            public string? Sha1 { get; set; }

            public string? Btih { get; set; }
            public string? Summation { get; set; }
            public string? Format { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
            public decimal? Length { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
            public int? Width { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
            public int? Height { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
            public int? Rotation { get; set; }

            [JsonPropertyName("viruscheck")]
            [JsonConverter(typeof(UnixEpochDateTimeNullableConverter))]
            public DateTimeOffset? VirusCheckDate { get; set; }
        }

        public JsonDocument? Metadata { get; set; }

        [JsonPropertyName("item_last_updated")]
        [JsonConverter(typeof(UnixEpochDateTimeNullableConverter))]
        public DateTimeOffset? DateLastUpdated { get; set; }

        [JsonPropertyName("item_size")]
        public long Size { get; set; }

        public long Uniq { get; set; }

        [JsonPropertyName("servers_unavailable")]
        public bool? ServersUnavailable { get; set; }

        [JsonPropertyName("workable_servers")]
        [JsonConverter(typeof(EnumerableStringConverter))]
        public IEnumerable<string>? WorkableServers { get; set; }

        public void Dispose()
        {
            Metadata?.Dispose();
            Metadata = null;
            GC.SuppressFinalize(this);
        }
    }

    public async Task<ReadResponse> ReadAsync(string identifier)
    {
        return await _client.GetAsync<ReadResponse>(Url(identifier));
    }

    public class WriteResponse : ServerResponse
    {
        [JsonPropertyName("task_id")]
        public long? TaskId { get; set; }

        public string? Log { get; set; }
        public string? Error { get; set; }
    }

    internal async Task<WriteResponse?> WriteAsync(string url, string target, string json)
    {
        var formData = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("-target", target),
            new KeyValuePair<string, string>("-patch", json)
        };

        var httpContent = new FormUrlEncodedContent(formData);
        var writeMetadataResponse = await _client.SendAsync<WriteResponse>(HttpMethod.Post, url, httpContent);

        writeMetadataResponse?.EnsureSuccess();
        return writeMetadataResponse;
    }

    public async Task<WriteResponse?> WriteAsync(string identifier, JsonPatchDocument patch)
    {
        var json = JsonSerializer.Serialize(patch.Operations);
        return await WriteAsync(Url(identifier), "metadata", json);
    }
}