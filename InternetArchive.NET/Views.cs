namespace InternetArchive;

public class Views
{
    private const string Url = "https://be-api.us.archive.org/views/v1";
    private static string DetailsUrl<T>(string type, string id, T startDate, T endDate) => $"{Url}/detail/{type}/{UrlEncode(id)}/{startDate:yyyy-MM-dd}/{endDate:yyyy-MM-dd}";

    private readonly Client _client;
    public Views(Client client)
    {
        _client = client;
    }

    public class Summary
    {
        [JsonPropertyName("have_data")]
        public bool? HasData { get; set; }

        [JsonPropertyName("last_7day")]
        public long? Last7Days { get; set; }

        [JsonPropertyName("last_30day")]
        public long? Last30Days { get; set; }

        [JsonPropertyName("all_time")]
        public long? AllTime { get; set; }

        public SummaryDetail? Detail { get; set; }
    }

    public class SummaryDetail
    {
        [JsonPropertyName("pre_20170101_total")]
        public long? Pre2017Total { get; set; }

        [JsonPropertyName("non_robot")]
        public SummaryDetailStats? NonRobot { get; set; }

        public SummaryDetailStats? Robot { get; set; }
        public SummaryDetailStats? Unrecognized { get; set; }
        public SummaryDetailStats? Pre2017 { get; set; }
    }

    public class SummaryDetailStats
    {
        [JsonPropertyName("per_day")]
        public IEnumerable<long> PerDay { get; set; } = Enumerable.Empty<long>();

        [JsonPropertyName("previous_days_total")]
        public long? PreviousDaysTotal { get; set; }

        [JsonPropertyName("sum_per_day_data")]
        public long? SumPerDay { get; set; }
    }

    public async Task<Summary> GetItemSummaryAsync(string identifier, bool legacy = false)
    {
        var summaries = await GetItemSummaryAsync(new[] { identifier }, legacy).ConfigureAwait(false);
        if (summaries.Count == 0) throw new Exception("identifier not found");
        return summaries.First().Value;
    }

    public async Task<Dictionary<string, Summary>> GetItemSummaryAsync(IEnumerable<string> identifiers, bool legacy = false)
    {
        string api = legacy ? "legacy_counts" : "short";
        return await _client.GetAsync<Dictionary<string, Summary>>($"{Url}/{api}/{string.Join(",", identifiers)}").ConfigureAwait(false);
    }

    public class SummaryPerDay<T>
    {
        public IEnumerable<T> Days { get; set; } = Enumerable.Empty<T>();
        public Dictionary<string, Summary> Ids { get; set; } = new();
    }

    public async Task<SummaryPerDay<T>> GetItemSummaryPerDayAsync<T>(string identifier)
    {
        return await GetItemSummaryPerDayAsync<T>(new[] { identifier }).ConfigureAwait(false);
    }

    public async Task<SummaryPerDay<T>> GetItemSummaryPerDayAsync<T>(IEnumerable<string> identifiers)
    {
        return await _client.GetAsync<SummaryPerDay<T>>($"{Url}/long/{string.Join(",", identifiers)}").ConfigureAwait(false);
    }

    public class Details<T>
    {
        [JsonPropertyName("counts_geo")]
        public IEnumerable<GeoCount> Counts { get; set; } = Enumerable.Empty<GeoCount>();
        public IEnumerable<T> Days { get; set; } = Enumerable.Empty<T>();

        public IEnumerable<Referer_> Referers { get; set; } = Enumerable.Empty<Referer_>();

        public class GeoCount
        {
            [JsonPropertyName("count_kind")]
            public string? CountKind { get; set; }

            public string? Country { get; set; }

            [JsonPropertyName("geo_country")]
            public string? GeoCountry { get; set; }

            [JsonPropertyName("geo_state")]
            public string? GeoState { get; set; }

            [JsonPropertyName("lat")]
            public decimal? Latitude { get; set; }

            [JsonPropertyName("lng")]
            public decimal? Longitude { get; set; }

            public string? State { get; set; }

            [JsonPropertyName("sum_count_value")]
            public long? Count { get; set; }

            [JsonPropertyName("ua_kind")]
            public string? Kind { get; set; }
        }

        public class Referer_
        {
            public string? Referer { get; set; }
            public long? Score { get; set; }

            [JsonPropertyName("ua_kind")]
            public string? Kind { get; set; }
        }
    }

    public async Task<Details<T>> GetItemDetailsAsync<T>(string identifier, T startDate, T endDate)
    {
        return await _client.GetAsync<Details<T>>(DetailsUrl("item", identifier, startDate, endDate)).ConfigureAwait(false);
    }

    public async Task<Details<T>> GetCollectionDetailsAsync<T>(string collection, T startDate, T endDate)
    {
        return await _client.GetAsync<Details<T>>(DetailsUrl("collection", collection, startDate, endDate)).ConfigureAwait(false);
    }

    // documented but not currently implemented at archive.org
    internal async Task<Details<T>> GetContributorDetailsAsync<T>(string contributor, T startDate, T endDate)
    {
        return await _client.GetAsync<Details<T>>(DetailsUrl("contributor", contributor, startDate, endDate)).ConfigureAwait(false);
    }
}