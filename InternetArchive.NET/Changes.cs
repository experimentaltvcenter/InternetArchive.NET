namespace InternetArchive;

public class Changes(Client client)
{
    private readonly string Url = "https://be-api.us.archive.org/changes/v1";

    private readonly Client _client = client;

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
            return Changes?.Select(x => x.Identifier) ?? [];
        }
    }

    private async Task<GetResponse> GetHelperAsync(CancellationToken cancellationToken, string? token = null, DateTime? startDate = null, bool? fromBeginning = null)
    {
        var formData = new List<KeyValuePair<string, string>>
        {
            new("access", _client.AccessKey),
            new("secret", _client.SecretKey)
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
        return await _client.SendAsync<GetResponse>(HttpMethod.Post, Url, httpContent, cancellationToken).ConfigureAwait(false)
            ?? throw new Exception("null response from server");
    }

    public async Task<GetResponse> GetFromBeginningAsync(CancellationToken cancellationToken = default)
    {
        return await GetHelperAsync(cancellationToken, fromBeginning: true).ConfigureAwait(false);
    }

    public async Task<GetResponse> GetStartingNowAsync(CancellationToken cancellationToken = default)
    {
        return await GetHelperAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<GetResponse> GetAsync(string token, CancellationToken cancellationToken = default)
    {
        return await GetHelperAsync(cancellationToken, token).ConfigureAwait(false);
    }

    public async Task<GetResponse> GetAsync(DateTime startDate, CancellationToken cancellationToken = default)
    {
        return await GetHelperAsync(cancellationToken, startDate: startDate).ConfigureAwait(false);
    }

#if NET
    public async Task<GetResponse> GetAsync(DateOnly startDate, CancellationToken cancellationToken = default)
    {
        return await GetAsync(new DateTime(startDate.Year, startDate.Month, startDate.Day), cancellationToken).ConfigureAwait(false);
    }
#endif
}