namespace InternetArchive;

public class Wayback
{
    private const string WaybackUrl = "https://archive.org/wayback/available";
    private const string CdxUrl = "https://web.archive.org/cdx/search/cdx";

    internal static readonly string DateFormat = "yyyyMMddHHmmss";

    private readonly Client _client;
    public Wayback(Client client)
    {
        _client = client;
    }

    public class IsAvailableResponse
    {
        [JsonPropertyName("available")]
        public bool IsAvailable { get; set; }

        public string? Url { get; set; }

        [JsonConverter(typeof(WaybackDateTimeOffsetNullableConverter))]
        public DateTimeOffset? Timestamp { get; set; }

        [JsonConverter(typeof(NullableHttpStatusCodeConverter))]
        public HttpStatusCode? Status { get; set; }
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

    public async Task<IsAvailableResponse> IsAvailableAsync(string url, DateTimeOffset? timestamp = null, CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string> { { "url", url } };
        if (timestamp.HasValue) query.Add("timestamp", timestamp.Value.ToString(DateFormat));

        var response = await _client.GetAsync<WaybackResponse>(WaybackUrl, query, cancellationToken).ConfigureAwait(false);
        return response.ArchivedSnapshots?.IsAvailableResponse ?? new IsAvailableResponse();
    }

    public class SearchRequest
    {
        public string? Url { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public string? MatchType { get; set; }
        public string? Collapse { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public bool FastLatest { get; set; }
        public string? ResumeKey { get; set; }

        internal Dictionary<string, string> ToQuery()
        {
            if (Url == null) throw new InternetArchiveException("Url is required");
            var query = new Dictionary<string, string> { { "url", Url }, { "showResumeKey", "true" } };

            if (StartTime.HasValue) query.Add("from", StartTime.Value.ToString(DateFormat));
            if (EndTime.HasValue) query.Add("to", EndTime.Value.ToString(DateFormat));
            if (MatchType != null) query.Add("matchType", MatchType);
            if (Collapse != null) query.Add("collapse", Collapse);
            if (Limit.HasValue) query.Add("limit", Limit.Value.ToString());
            if (Offset.HasValue) query.Add("offset", Offset.Value.ToString());
            if (Page.HasValue) query.Add("page", Page.Value.ToString());
            if (PageSize.HasValue) query.Add("pageSize", PageSize.Value.ToString());
            if (FastLatest) query.Add("fastLatest", "true");
            if (ResumeKey != null) query.Add("resumeKey", ResumeKey);

            return query;
        }
    }

    public class SearchResponse
    {
        public List<CdxResponse> Results { get; set; } = new();
        public string? ResumeKey { get; set; }

        public class CdxResponse
        {
            public string UrlKey { get; set; } = null!;
            public DateTimeOffset Timestamp { get; set; }
            public string Original { get; set; } = null!;
            public string MimeType { get; set; } = null!;
            public HttpStatusCode? StatusCode { get; set; }
            public string Digest { get; set; } = null!;
            public long Length { get; set; }
        }
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _client.GetAsync<string>(CdxUrl, request.ToQuery(), cancellationToken).ConfigureAwait(false);
        var response = new SearchResponse();

        bool lastLine = false;
        foreach (var line in result.Split('\n'))
        {
            if (line.Length == 0)
            {
                lastLine = true;
                continue;
            }

            if (lastLine == true)
            {
                response.ResumeKey = line;
                break;
            }

            var fields = line.Split(new char[] { ' ' }, 8);
            if (fields.Length != 7) throw new InternetArchiveException("Unexpected number of fields returned from server");

            var cdxResponse = new SearchResponse.CdxResponse
            {
                UrlKey = fields[0],
                Timestamp = DateTimeOffset.ParseExact(fields[1], DateFormat, CultureInfo.InvariantCulture.DateTimeFormat),
                Original = fields[2],
                MimeType = fields[3],
                Digest = fields[5],
                Length = long.Parse(fields[6])
            };

            if (int.TryParse(fields[4], out var statusCode)) cdxResponse.StatusCode = (HttpStatusCode)statusCode; // can be "-"
            response.Results.Add(cdxResponse);
        }

        return response;
    }

    public async Task<int?> GetNumPagesAsync(string url, CancellationToken cancellationToken = default)
    {
        var query = new SearchRequest { Url = url }.ToQuery();
        query.Add("showNumPages", "true");

        var response = await _client.GetAsync<string>(CdxUrl, query, cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(response);
    }
}