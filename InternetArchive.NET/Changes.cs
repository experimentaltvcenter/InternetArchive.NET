namespace InternetArchive;

public class Changes
{
    private readonly string Url = "https://be-api.us.archive.org/changes/v1";

    private readonly Client _client;
    public Changes(Client client)
    {
        _client = client;
    }

    public class GetResponse
    {
        [JsonPropertyName("estimated_distance_from_head")]
        public int? EstimatedDistanceFromHead { get; set; }

        [JsonPropertyName("do_sleep_before_returning")]
        public bool? SleepBeforeReturning { get; set; }

        public class Change
        {
            public string Identifier { get; set; } = null!;
        }

        public IEnumerable<Change>? Changes { get; set; }

        [JsonPropertyName("next_token")]
        public string? Token { get; set; }

        public IEnumerable<string> Identifiers()
        {
            return Changes?.Select(x => x.Identifier) ?? Enumerable.Empty<string>();
        }
    }

    private async Task<GetResponse> GetHelperAsync(string? token = null, DateTime? startDate = null, bool? fromBeginning = null)
    {
        var formData = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("access", _client.AccessKey),
            new KeyValuePair<string, string>("secret", _client.SecretKey)
        };

        if (token != null) formData.Add(new KeyValuePair<string, string>("token", token));

        if (startDate != null)
        {
            formData.Add(new KeyValuePair<string, string>("start_date", $"{startDate:yyyyMMdd}"));
        }
        else if (fromBeginning == true)
        {
            formData.Add(new KeyValuePair<string, string>("start_date", "0"));
        }

        var httpContent = new FormUrlEncodedContent(formData);
        var response = await _client.SendAsync<GetResponse>(HttpMethod.Post, Url, httpContent).ConfigureAwait(false);
        if (response == null) throw new Exception("null response from server");

        return response;
    }

    public async Task<GetResponse> GetFromBeginningAsync()
    {
        return await GetHelperAsync(fromBeginning: true).ConfigureAwait(false);
    }

    public async Task<GetResponse> GetStartingNowAsync()
    {
        return await GetHelperAsync().ConfigureAwait(false);
    }

    public async Task<GetResponse> GetAsync(string token)
    {
        return await GetHelperAsync(token).ConfigureAwait(false);
    }

    public async Task<GetResponse> GetAsync(DateTime startDate)
    {
        return await GetHelperAsync(startDate: startDate).ConfigureAwait(false);
    }

#if NET
    public async Task<GetResponse> GetAsync(DateOnly startDate)
    {
        return await GetAsync(new DateTime(startDate.Year, startDate.Month, startDate.Day)).ConfigureAwait(false);
    }
#endif
}