namespace InternetArchive;

public class Metadata(Client client)
{
    internal static string Url(string identifier) => $"https://archive.org/metadata/{identifier}";

    private readonly Client _client = client;

    public class ReadResponse
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

        [JsonPropertyName("is_dark")]
        public bool? IsDark { get; set; }

        public string Dir { get; set; } = null!;

        public IEnumerable<File> Files { get; set; } = [];

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

        public JsonElement? Metadata { get; set; }

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
    }

    public async Task<ReadResponse> ReadAsync(string identifier, CancellationToken cancellationToken = default)
    {
        return await _client.GetAsync<ReadResponse>(Url(identifier), cancellationToken).ConfigureAwait(false);
    }

    public class WriteResponse : ServerResponse
    {
        [JsonPropertyName("task_id")]
        public long? TaskId { get; set; }

        public string? Log { get; set; }
        public string? Error { get; set; }
    }

    internal async Task<WriteResponse?> WriteAsync(string url, string target, string json, CancellationToken cancellationToken)
    {
        var formData = new List<KeyValuePair<string, string>>
        {
            new("-target", target),
            new("-patch", json)
        };

        var httpContent = new FormUrlEncodedContent(formData);
        var writeMetadataResponse = await _client.SendAsync<WriteResponse>(HttpMethod.Post, url, httpContent, cancellationToken).ConfigureAwait(false);

        writeMetadataResponse?.EnsureSuccess();
        return writeMetadataResponse;
    }

    public async Task<WriteResponse?> WriteAsync(string identifier, JsonPatchDocument patch, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(patch.Operations);
        return await WriteAsync(Url(identifier), "metadata", json, cancellationToken).ConfigureAwait(false);
    }
}