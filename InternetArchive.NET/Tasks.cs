namespace InternetArchive;

public class Tasks
{
    private readonly string Url = "https://archive.org/services/tasks.php";
    private readonly string LogUrl = "https://catalogd.archive.org/services/tasks.php";

    private readonly Client _client;
    public Tasks(Client client)
    {
        _client = client;
    }

    public class GetResponse : ServerResponse
    {
        public Value_? Value { get; set; }

        public class Value_
        {
            public class Summary_
            {
                public int? Queued { get; set; }
                public int? Running { get; set; }
                public int? Error { get; set; }
                public int? Paused { get; set; }
            }

            public Summary_? Summary { get; set; }

            public class HistoryEntry
            {
                public string? Identifier { get; set; }

                [JsonPropertyName("task_id")]
                public long? TaskId { get; set; }

                public string? Server { get; set; }

                [JsonPropertyName("cmd")]
                public string? Command { get; set; }

                public Dictionary<string, string> Args { get; set; } = new();

                [JsonConverter(typeof(DateTimeNullableConverter))]
                [JsonPropertyName("submittime")]
                public DateTime? DateSubmitted { get; set; }

                public string? Submitter { get; set; }
                public int Priority { get; set; }

                public long? Finished { get; set; } // not a Unix timestamp
            }

            public List<HistoryEntry>? History { get; set; }
        }

        public string? Cursor { get; set; }
    }

    public enum SubmitTimeType
    {
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual
    }

    public class GetRequest
    {
        public string? Identifier { get; set; }
        public long? TaskId { get; set; }
        public string? Server { get; set; }
        public string? Command { get; set; }
        public string? Args { get; set; }
        public string? Submitter { get; set; }
        public int? Priority { get; set; }
        public int? WaitAdmin { get; set; }
        public SubmitTimeType? SubmitTimeType { get; set; }
        public DateTimeOffset? SubmitTime { get; set; }
        public bool Summary { get; set; } = true;
        public bool? Catalog { get; set; }
        public bool? History { get; set; }
        public int? Limit { get; set; }
    }

    public async Task<GetResponse> GetAsync(GetRequest request)
    {
        var query = new Dictionary<string, string>();

        if (request.Identifier != null) query.Add("identifier", request.Identifier);
        if (request.TaskId != null) query.Add("task_id", request.TaskId.Value.ToString());
        if (request.Server != null) query.Add("server", request.Server);
        if (request.Command != null) query.Add("cmd", request.Command);
        if (request.Args != null) query.Add("args", request.Args);
        if (request.Submitter != null) query.Add("submitter", request.Submitter);
        if (request.Priority != null) query.Add("priority", request.Priority.Value.ToString());
        if (request.WaitAdmin != null) query.Add("wait_admin", request.WaitAdmin.Value.ToString());

        string? submitTimeType = request.SubmitTimeType switch
        {
            SubmitTimeType.GreaterThan => ">",
            SubmitTimeType.GreaterThanOrEqual => ">=",
            SubmitTimeType.LessThan => "<",
            SubmitTimeType.LessThanOrEqual => "<=",
            null => null,
            _ => throw new Exception($"Unexpected SubmitTimeType: {request.SubmitTimeType}")
        };

        if (request.SubmitTime != null && submitTimeType == null) throw new Exception($"Must specify a SubmitTimeType");
        if (submitTimeType != null && request.SubmitTime == null) throw new Exception($"Specified a SubmitTimeType but no SubmitTime");
        if (request.SubmitTime != null) query.Add($"submittime{submitTimeType}", request.SubmitTime.Value.ToString("O"));

        if (request.Summary == false) query.Add("summary", "0");
        if (request.Catalog == true) query.Add("catalog", "1");
        if (request.History == true) query.Add("history", "1");

        var response = await _client.GetAsync<GetResponse>(Url, query);
        response.EnsureSuccess();
        return response;
    }

    public enum Command
    {
        BookOp,
        Backup,
        Delete,
        Derive,
        Fixer,
        MakeDark,
        MakeUndark,
        Rename
    }

    private class SubmitRequest
    {
        public string? Identifier { get; set; }

        [JsonPropertyName("cmd")]
        public string? Command { get; set; }

        public Dictionary<string, string>? Args { get; set; }
        public int? Priority { get; set; }
    }

    public class SubmitResponse : ServerResponse
    {
        public Value_? Value { get; set; }

        public class Value_
        {
            [JsonPropertyName("task_id")]
            public long? TaskId { get; set; }

            public string? Log { get; set; }
        }
    }

    private static readonly Dictionary<Command, string> _submitCommands = new()
    {
        { Command.BookOp, "book_op.php" },
        { Command.Backup, "bup.php" },
        { Command.Delete, "delete.php" },
        { Command.Derive, "derive.php" },
        { Command.Fixer, "fixer.php" },
        { Command.MakeDark, "make_dark.php" },
        { Command.MakeUndark, "make_undark.php" },
        { Command.Rename, "rename.php" }
    };

    public async Task<SubmitResponse?> SubmitAsync(string identifier, Command command, Dictionary<string, string>? args = null, int? priority = null)
    {
        var request = new SubmitRequest
        {
            Identifier = identifier,
            Command = _submitCommands[command],
            Args = args,
            Priority = priority
        };

        var response = await _client.SendAsync<SubmitResponse>(HttpMethod.Post, Url, request);
        response?.EnsureSuccess();
        return response;
    }

    public class RateLimitResponse : ServerResponse
    {
        public Value_? Value { get; set; }

        public class Value_
        {
            [JsonPropertyName("cmd")]
            public string Command { get; set; } = "";

            [JsonPropertyName("task_limits")]
            public int TaskLimits { get; set; }

            [JsonPropertyName("tasks_inflight")]
            public int TasksInFlight { get; set; }

            [JsonPropertyName("tasks_blocked_by_offline")]
            public int TasksBlockedByOffline { get; set; }
        }
    }

    public async Task<RateLimitResponse> GetRateLimitAsync(Command command)
    {
        var query = new Dictionary<string, string>
        {
            { "rate_limits", "1" },
            { "cmd", command.ToString() }
        };

        var response = await _client.GetAsync<RateLimitResponse>(Url, query);
        response.EnsureSuccess();
        return response;
    }

    public class RerunRequest
    {
        public string Op { get; set; } = "rerun";
        public long TaskId { get; set; }
    }

    public class RerunResponse : ServerResponse
    {
        public Dictionary<string, object?> Value { get; set; } = new();
    }

    public async Task<RerunResponse?> RerunAsync(long taskId)
    {
        var request = new RerunRequest { TaskId = taskId };
        var response = await _client.SendAsync<RerunResponse>(HttpMethod.Put, Url, request);
        response?.EnsureSuccess();
        return response;
    }

    public enum RunState
    {
        Queued = 0,
        Running = 1,
        Error = 2,
        Paused = 9
    }

    public class GetLogRequest
    {
        [JsonPropertyName("identifier")]
        public string? Identifier { get; set; }

        [JsonPropertyName("cmd")]
        public int? Command { get; set; }

        [JsonPropertyName("args")]
        public Dictionary<string, string> Args { get; set; } = new();

        [JsonPropertyName("priority")]
        public int Priority { get; set; } = 0;
    }

    public async Task<GetLogRequest> GetLogAsync(long taskId)
    {
        var query = new Dictionary<string, string> { { "task_log", $"{taskId}" } };
        return await _client.GetAsync<GetLogRequest>(LogUrl, query);
    }
}