namespace InternetArchive;

public class Relationships(Client client)
{
    private static string Url(string identifier) => $"https://archive.org/metadata/{identifier}/simplelists";
    private static readonly string SearchUrl = "https://archive.org/advancedsearch.php";

    private readonly Client _client = client;

    public class GetParentsResponse
    {
        public Dictionary<string, SimpleList> Lists { get; set; } = [];
        public string? Error { get; set; }
    }

    public class SimpleList
    {
        public JsonElement? Notes { get; set; }

        [JsonPropertyName("sys_changed_by")]
        public LastChangedBy_? LastChangedBy { get; set; }

        public class LastChangedBy_
        {
            public string? Source { get; set; }
            public string? Username { get; set; }

            [JsonPropertyName("task_id")]
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
            public long? TaskId { get; set; }
        }

        [JsonPropertyName("sys_last_changed")]
        [JsonConverter(typeof(DateTimeNullableConverter))]
        public DateTime? LastChangedDate { get; set; }
    }

    internal class SimpleListResponse
    {
        public Dictionary<string, Dictionary<string, SimpleList>>? Result { get; set; }
        public string? Error { get; set; }
    }

    public async Task<GetParentsResponse> GetParentsAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var simpleListResponse = await _client.GetAsync<SimpleListResponse>(Url(identifier), cancellationToken).ConfigureAwait(false);

        var response = new GetParentsResponse { Error = simpleListResponse.Error };
        if (simpleListResponse.Result != null) response.Lists = simpleListResponse.Result.Values.Single();

        return response;
    }

    public class GetChildrenResponse
    {
        public Response_? Response { get; set; }

        public class Response_
        {
            public long? NumFound { get; set; }
            public long? Start { get; set; }
            public IEnumerable<Doc>? Docs { get; set; }

            public class Doc
            {
                public string? Identifier { get; set; } = null!;
            }
        }

        public IEnumerable<string?> Identifiers()
        {
            return Response?.Docs?.Select(x => x.Identifier) ?? [];
        }
    }

    public async Task<GetChildrenResponse> GetChildrenAsync(string identifier, string? listname = null, int? rows = null, int? page = null, CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string>
        {
            { "q", $"simplelists__{listname ?? "catchall"}:{identifier}" },
            { "fl", "identifier" },
            { "output", "json" },
            { "rows", rows == null ? "*" : rows.Value.ToString() }
        };

        if (page.HasValue) query.Add("page", page.Value.ToString());

        return await _client.GetAsync<GetChildrenResponse>(SearchUrl, query, cancellationToken).ConfigureAwait(false);
    }

    private class Patch
    {
        public string? Op { get; set; }
        public string? Parent { get; set; }
        public string? List { get; set; }
        public string? Notes { get; set; }
    }

    public async Task<Metadata.WriteResponse?> AddAsync(string identifier, string parentIdentifier, string listName, string? notes = null, CancellationToken cancellationToken = default)
    {
        var patch = new Patch
        {
            Op = "set", // set is not a standard verb, so we can't use JsonPatchDocument
            Parent = parentIdentifier,
            List = listName,
            Notes = notes
        };

        return await _client.Metadata.WriteAsync(Metadata.Url(identifier), "simplelists", JsonSerializer.Serialize(patch, _jsonSerializerOptions), cancellationToken).ConfigureAwait(false);
    }

    public async Task<Metadata.WriteResponse?> RemoveAsync(string identifier, string parentIdentifier, string listName, CancellationToken cancellationToken = default)
    {
        var patch = new Patch
        {
            Op = "delete", // delete is not a standard verb either (should be "remove")
            Parent = parentIdentifier,
            List = listName,
        };

        return await _client.Metadata.WriteAsync(Metadata.Url(identifier), "simplelists", JsonSerializer.Serialize(patch, _jsonSerializerOptions), cancellationToken).ConfigureAwait(false);
    }
}