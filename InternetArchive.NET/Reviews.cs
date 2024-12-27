namespace InternetArchive;

public class Reviews(Client client)
{
    private static string Url(string identifier) => $"https://archive.org/services/reviews.php?identifier={identifier}";

    private readonly Client _client = client;

    public class GetResponse : ServerResponse
    {
        public Value_? Value { get; set; }

        public class Value_
        {
            [JsonPropertyName("reviewtitle")]
            public string? Title { get; set; }

            [JsonPropertyName("reviewbody")]
            public string? Body { get; set; }

            public string? Reviewer { get; set; }

            [JsonPropertyName("reviewer_itemname")]
            public string? ReviewerItemName { get; set; }

            [JsonPropertyName("createdate")]
            [JsonConverter(typeof(DateTimeNullableConverter))]
            public DateTime? DateCreated { get; set; }

            [JsonPropertyName("reviewdate")]
            [JsonConverter(typeof(DateTimeNullableConverter))]
            public DateTime? DateModified { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
            public int? Stars { get; set; }
        }
    }

    public async Task<GetResponse> GetAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetAsync<GetResponse>(Url(identifier), cancellationToken).ConfigureAwait(false);
        response.EnsureSuccess();
        return response;
    }

    public class AddOrUpdateResponse  : ServerResponse
    {
        public Value_? Value { get; set; }

        public class Value_
        {
            [JsonPropertyName("task_id")]
            public long? TaskId { get; set; }
            [JsonPropertyName("review_updated")]
            public bool? ReviewUpdated { get; set; }
        }
    }

    public class AddOrUpdateRequest
    {
        [JsonIgnore]
        public string? Identifier { get; set; }

        public string? Title { get; set; }
        public string? Body { get; set; }
        public int? Stars { get; set; }
    }

    public async Task<AddOrUpdateResponse?> AddOrUpdateAsync(AddOrUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Identifier == null) throw new Exception("identifier required");

        var response = await _client.SendAsync<AddOrUpdateResponse>(HttpMethod.Post, Url(request.Identifier), request, cancellationToken).ConfigureAwait(false);
        response?.EnsureSuccess();
        return response;
    }

    public class DeleteResponse : ServerResponse
    {
        public Value_? Value { get; set; }

        public class Value_
        {
            [JsonPropertyName("task_id")]
            public long? TaskId { get; set; }
        }
    }

    public async Task<DeleteResponse?> DeleteAsync(string identifier, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage
        {
            RequestUri = new Uri(Url(identifier)),
            Method = HttpMethod.Delete
        };

        var response = await _client.SendAsync<DeleteResponse>(httpRequest, cancellationToken).ConfigureAwait(false);
        response?.EnsureSuccess();
        return response;
    }
}
