namespace InternetArchive;

public class Search
{
    private readonly string Url = "https://archive.org/services/search/v1/scrape";

    private readonly Client _client;
    public Search(Client client)
    {
        _client = client;
    }

    public class ScrapeRequest
    {
        public string? Query { get; set; }
        public IEnumerable<string>? Sorts { get; set; }
        public IEnumerable<string>? Fields { get; set; }
        public int? Count { get; set; }
        public string? Cursor { get; set; }
        public bool TotalOnly { get; set; }
    }

    public class ScrapeResponse
    {
        public IEnumerable<ScrapeResponseItem> Items { get; set; } = Enumerable.Empty<ScrapeResponseItem>();
        public int? Count { get; set; }
        public string? Cursor { get; set; }
        public long? Total { get; set; }
    }

    public class ScrapeResponseItem
    {
        [JsonPropertyName("avg_rating")]
        public int? AverageRating { get; set; }

        public DateTimeOffset? AddedDate { get; set; }

        [JsonPropertyName("backup_location")]
        public string? BackupLocation { get; set; }

        public string? Btih { get; set; }

        [JsonPropertyName("call_number")]
        public string? CallNumber { get; set; }

        [JsonPropertyName("collection")]
        [JsonConverter(typeof(EnumerableStringConverter))]
        public IEnumerable<string>? Collections { get; set; }

        public string? Contributor { get; set; }
        public string? Coverage { get; set; }
        public string? Creator { get; set; }
        public string? Date { get; set; }

        [JsonConverter(typeof(EnumerableStringConverter))]
        [JsonPropertyName("description")]
        public IEnumerable<string>? Descriptions { get; set; }

        public long? Downloads { get; set; }

        [JsonPropertyName("external-identifier")]
        [JsonConverter(typeof(EnumerableStringConverter))]
        public IEnumerable<string>? ExternalIdentifiers { get; set; }

        public int? FilesCount { get; set; }
        public int? FoldoutCount { get; set; }

        [JsonPropertyName("format")]
        [JsonConverter(typeof(EnumerableStringConverter))]
        public IEnumerable<string>? Formats { get; set; }

        public string? Genre { get; set; }
        public string? Identifier { get; set; }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? ImageCount { get; set; }
        
        public string? IndexFlag { get; set; }

        [JsonPropertyName("item_size")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? ItemSize { get; set; }

        [JsonPropertyName("language")]
        [JsonConverter(typeof(EnumerableStringConverter))]
        public IEnumerable<string>? Languages { get; set; }

        public string? LicenseUrl { get; set; }
        public string? MediaType { get; set; }
        public string? Members { get; set; }
        public string? Month { get; set; }
        public string? Name { get; set; }
        public string? NoIndex { get; set; }

        [JsonPropertyName("num_reviews")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] 
        public int? NumReviews { get; set; }

        [JsonPropertyName("oai_updatedate")]
        public IEnumerable<DateTimeOffset>? OaiUpdateDate { get; set; }

        [JsonPropertyName("primary_collection")]
        public string? PrimaryCollection { get; set; }

        public DateTimeOffset? PublicDate { get; set; }
        public string? Publisher { get; set; }

        [JsonPropertyName("related-external-id")]
        [JsonConverter(typeof(EnumerableStringConverter))]
        public IEnumerable<string>? RelatedExternalIds { get; set; }

        [JsonPropertyName("reported-server")]
        public string? ReportedServer { get; set; }

        [JsonPropertyName("reviewdate")]
        public DateTimeOffset? ReviewDate { get; set; }

        public string? Rights { get; set; }
        public string? Scanner { get; set; }
        public string? ScanningCentre { get; set; }
        public string? Source { get; set; }

        [JsonPropertyName("stripped_tags")]
        public string? StrippedTags { get; set; }

        [JsonPropertyName("subject")]
        [JsonConverter(typeof(EnumerableStringConverter))]
        public IEnumerable<string>? Subjects { get; set; }

        public string? Title { get; set; }
        public string? Type { get; set; }
        public string? Volume { get; set; }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? Week { get; set; }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? Year { get; set; }
    }

    internal Dictionary<string, string> ScrapeHelper(ScrapeRequest request)
    {
        var query = new Dictionary<string, string>();

        if (request.Query != null) query.Add("q", request.Query);
        if (request.Fields != null) query.Add("fields", string.Join(",", request.Fields));
        if (request.Count != null) query.Add("count", request.Count.Value.ToString());
        if (request.TotalOnly == true) query.Add("total_only", "true");

        var sorts = request.Sorts;
        if (sorts?.Contains("identifier", StringComparer.OrdinalIgnoreCase) == true && sorts.Count() > 1)
        {
            // if identifier is specified, it must be last
            sorts = request.Sorts!.Where(x => !x.Equals("identifier", StringComparison.OrdinalIgnoreCase)).Append("identifier");
        }
        if (sorts != null) query.Add("sorts", string.Join(",", sorts));

        return query;
    }

    public async Task<ScrapeResponse> ScrapeAsync(ScrapeRequest request)
    {
        var query = ScrapeHelper(request);
        return await _client.GetAsync<ScrapeResponse>(Url, query).ConfigureAwait(false);
    }

    public async Task<JsonDocument> ScrapeAsJsonAsync(ScrapeRequest request)
    {
        var query = ScrapeHelper(request);
        return await _client.GetAsync<JsonDocument>(Url, query).ConfigureAwait(false);
    }
}