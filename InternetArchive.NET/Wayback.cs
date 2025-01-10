namespace InternetArchive;

public class Wayback(Client client)
{
    private const string CdxUrl = "https://web.archive.org/cdx/search/cdx";
    private const string SavePageUrl = "https://web.archive.org/save";
    private static string SavePageGetJobStatusUrl(string jobId) => $"https://web.archive.org/save/status/{jobId}";
    private static string SavePageGetSystemStatusUrl => $"https://web.archive.org/save/status/system";

    internal static readonly string DateFormat = "yyyyMMddHHmmss";

    private readonly Client _client = client;

    public class SearchRequest
    {
        public string? Url { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public string? MatchType { get; set; }
        public string? Collapse { get; set; }
        public int? Limit { get; set; }
        [Obsolete("Support removed from archive.org in 2024")] public int? Offset { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public bool FastLatest { get; set; }
        public string? ResumeKey { get; set; }

        internal Dictionary<string, string> ToQuery()
        {
            if (Url == null) throw new InternetArchiveException("Url is required");
#pragma warning disable CS0618 // Type or member is obsolete
            if (Offset.HasValue) throw new InternetArchiveException("Offset is no longer supported");
#pragma warning restore CS0618

            var query = new Dictionary<string, string> { { "url", Url }, { "showResumeKey", "true" } };

            if (StartTime.HasValue) query.Add("from", StartTime.Value.ToString(DateFormat));
            if (EndTime.HasValue) query.Add("to", EndTime.Value.ToString(DateFormat));
            if (MatchType != null) query.Add("matchType", MatchType);
            if (Collapse != null) query.Add("collapse", Collapse);
            if (Limit.HasValue) query.Add("limit", Limit.Value.ToString());
            if (Page.HasValue) query.Add("page", Page.Value.ToString());
            if (PageSize.HasValue) query.Add("pageSize", PageSize.Value.ToString());
            if (FastLatest) query.Add("fastLatest", "true");
            if (ResumeKey != null) query.Add("resumeKey", ResumeKey);

            return query;
        }
    }

    public class SearchResponse
    {
        public List<CdxResponse> Results { get; set; } = [];
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

            var fields = line.Split([' '], 8);
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

    public class SavePageRequest
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("capture_all")]
        public bool? CaptureAll { get; set; }

        [JsonPropertyName("capture_outlinks")]
        public bool CaptureOutlinks { get; set; } = false;

        [JsonPropertyName("capture_screenshot")]
        public bool? CaptureScreenshot { get; set; }

        [JsonPropertyName("delay_wb_availability")]
        public bool? DelayAvailability { get; set; }

        [JsonPropertyName("force_get")]
        public bool? ForceGet { get; set; }

        [JsonPropertyName("skip_first_archive")]
        public bool SkipFirstArchive { get; set; } = true;

        [JsonPropertyName("if_not_archived_within")]
        public string? IfNotArchivedWithin { get; set; }

        [JsonPropertyName("outlinks_availability")]
        public bool? OutlinksAvailability { get; set; }

        [JsonPropertyName("email_result")]
        public bool? EmailResult { get; set; }

        [JsonPropertyName("js_behavior_timeout")]
        public int? JavascriptTimeout { get; set; }

        [JsonPropertyName("capture_cookie")]
        public string? CaptureCookie { get; set; }

        [JsonPropertyName("use_user_agent")]
        public string? UseUserAgent { get; set; }

        [JsonPropertyName("target_username")]
        public string? TargetUsername { get; set; }

        [JsonPropertyName("target_password")]
        public string? TargetPassword { get; set; }
    }

    public class SavePageResponse
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("job_id")]
        public string? JobId { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public async Task<SavePageResponse?> SavePageAsync(SavePageRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Url == null) throw new InternetArchiveException("Url is required");

        var form = new Dictionary<string, object>();
        foreach (var property in request.GetType().GetProperties())
        {
            var attribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            var key = attribute?.Name ?? throw new Exception("JsonPropertyNameAttribute is required");
            var value = property.GetValue(request);
            if (value == null) continue;

            if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
            {
                if ((bool)value == false) value = 0;
                else value = 1;
            }

            form.Add(key, value);
        }

        var requestHeaders = new Dictionary<string, string?> { { "Accept", "application/json" } };
        var response = await _client.SendAsync<SavePageResponse>(HttpMethod.Post, SavePageUrl, form, requestHeaders, cancellationToken).ConfigureAwait(false);

        return response;
    }

    public class SavePageStatusResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("exception")]
        public string? Exception { get; set; }

        [JsonPropertyName("status_ext")]
        public string? StatusException { get; set; }

        [JsonPropertyName("job_id")]
        public string? JobId { get; set; }

        [JsonPropertyName("original_url")]
        public string? OriginalUrl { get; set; }

        [JsonPropertyName("screenshot")]
        public string? Screenshot { get; set; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("duration_sec")]
        public double? DurationSeconds { get; set; }

        [JsonPropertyName("resources")]
        public IEnumerable<string>? Resources { get; set; }

        public class Outlink
        {
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonPropertyName("job_id")]
            public string? JobId { get; set; }
        }

        [JsonPropertyName("outlinks")]
        public IEnumerable<Outlink>? Outlinks { get; set; }
    }

    public async Task<SavePageStatusResponse?> GetSavePageStatusAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetAsync<SavePageStatusResponse>(SavePageGetJobStatusUrl(jobId), cancellationToken).ConfigureAwait(false);
        if (response?.Status == "error") throw new InternetArchiveResponseException(response.StatusException ?? "error");
        return response;
    }

    public class SavePageSystemStatusResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("recent_captures")]
        public int? RecentCaptures { get; set; }
    }

    public async Task<SavePageSystemStatusResponse?> GetSavePageSystemStatusAsync(CancellationToken cancellationToken = default)
    {
        return await _client.GetAsync<SavePageSystemStatusResponse>(SavePageGetSystemStatusUrl, cancellationToken).ConfigureAwait(false);
    }
}
