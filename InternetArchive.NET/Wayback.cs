namespace InternetArchive;

public class Wayback
{
    private const string Url = "https://archive.org/wayback/available";
    internal static readonly string DateFormat = "yyyyMMddHHmmss";

    private readonly Client _client;
    public Wayback(Client client)
    {
        _client = client;
    }

    internal class WaybackResponse
    {
        [JsonPropertyName("archived_snapshots")]
        public ArchivedSnapshots_? ArchivedSnapshots { get; set; }

        internal class ArchivedSnapshots_
        {
            [JsonPropertyName("closest")]
            public IsAvailableResponse? IsAvailableResponse { get; set; }
        }
    }

    public class IsAvailableResponse
    {
        [JsonPropertyName("available")]
        public bool IsAvailable { get; set; }

        public string? Url { get; set; }

        [JsonConverter(typeof(WaybackDateTimeOffsetNullableConverter))]
        public DateTimeOffset? Timestamp { get; set; }

        [JsonConverter(typeof(NullableStringToIntConverter))]
        public int? Status { get; set; }
    }

    public async Task<IsAvailableResponse> IsAvailable(string url, DateTimeOffset? timestamp = null)
    {
        var query = new Dictionary<string, string> { { "url", url } };
        if (timestamp.HasValue) query.Add("timestamp", timestamp.Value.ToString(DateFormat));

        var response = await _client.GetAsync<WaybackResponse>(Url, query).ConfigureAwait(false);
        return response.ArchivedSnapshots?.IsAvailableResponse ?? new IsAvailableResponse();
    }
}