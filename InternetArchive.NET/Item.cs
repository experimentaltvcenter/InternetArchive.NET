namespace InternetArchive;

public class Item
{
    private readonly string Url = "https://s3.us.archive.org";

    private readonly Client _client;
    public Item(Client client)
    {
        _client = client;
    }

    public class PutRequest
    {
        public string? Bucket { get; set; }
        public string? LocalPath { get; set; }
        public string? RemoteFilename { get; set; }
        public IEnumerable<KeyValuePair<string, object?>> Metadata { get; set; } = Enumerable.Empty<KeyValuePair<string, object?>>();

        public bool CreateBucket { get; set; }
        public bool NoDerive { get; set; }
        public bool KeepOldVersion { get; set; }
        public bool DeleteExistingMetadata { get; set; }

        public string? SimulateError { get; set; }
    }

    public async Task<HttpResponseMessage?> PutAsync(PutRequest request)
    {
        if (request.Bucket == null) throw new Exception("identifier required");
        using var httpRequest = new HttpRequestMessage { Method = HttpMethod.Put };

        if (request.LocalPath == null)
        {
            httpRequest.RequestUri = new Uri($"{Url}/{request.Bucket}");
        }
        else
        {
            if (!File.Exists(request.LocalPath)) throw new FileNotFoundException("File not found", request.LocalPath);
            var fileInfo = new FileInfo(request.LocalPath);

            string remoteFilename = request.RemoteFilename ?? Path.GetFileName(request.LocalPath);
            var fs = new FileStream(request.LocalPath, FileMode.Open, FileAccess.Read);

            httpRequest.RequestUri = new Uri($"{Url}/{request.Bucket}/{remoteFilename}");
            httpRequest.Content = new StreamContent(fs);

            httpRequest.Headers.Add($"x-archive-size-hint", $"{fileInfo.Length}");
            httpRequest.Content.Headers.ContentMD5 = CalculateMD5(request.LocalPath);
        }

        AddMetadata(httpRequest, request.Metadata);

        if (request.CreateBucket == true) httpRequest.Headers.Add("x-archive-auto-make-bucket", "1");
        if (request.KeepOldVersion == true) httpRequest.Headers.Add("x-archive-keep-old-version", "1");
        if (request.NoDerive == true) httpRequest.Headers.Add("x-archive-queue-derive", "0");
        if (request.DeleteExistingMetadata == true) httpRequest.Headers.Add("x-archive-ignore-preexisting-bucket", "1");
        if (request.SimulateError != null) httpRequest.Headers.Add("x-archive-simulate-error", request.SimulateError);

        return await _client.SendAsync<HttpResponseMessage>(httpRequest);

        static void AddMetadata(HttpRequestMessage httpRequest, IEnumerable<KeyValuePair<string, object?>> metadata)
        {
            foreach (var group in metadata.GroupBy(x => x.Key))
            {
                int count = 0;
                foreach (var kv in group)
                {
                    string? val = kv.Value?.ToString();
                    if (val == null) throw new NullReferenceException();
                    if (val.Any(x => x > 127))
                    {
                        val = $"uri({Uri.EscapeDataString(val)})";
                    }

                    httpRequest.Headers.Add($"x-archive-meta{count++}-{kv.Key.Replace("_", "--")}", val);
                }
            }
        }

        static byte[] CalculateMD5(string path)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(path);
            return md5.ComputeHash(stream);
        }
    }

    public class DeleteRequest
    {
        public string? Bucket { get; set; }
        public bool KeepOldVersion { get; set; }
        public bool CascadeDelete { get; set; }
    }

    public async Task<HttpResponseMessage?> DeleteAsync(DeleteRequest request)
    {
        if (request.Bucket == null) throw new Exception("identifier required");

        using var httpRequest = new HttpRequestMessage
        {
            RequestUri = new Uri($"{Url}/{request.Bucket}"),
            Method = HttpMethod.Delete
        };

        if (request.KeepOldVersion == true) httpRequest.Headers.Add("x-archive-keep-old-version", "1");
        if (request.CascadeDelete == true) httpRequest.Headers.Add("x-archive-cascade-delete", "1");

        return await _client.SendAsync<HttpResponseMessage>(httpRequest);
    }

    public class UseLimitResponse
    {
        public string? Bucket { get; set; }
        public string? AccessKey { get; set; }

        [JsonPropertyName("over_limit")]
        public int? OverLimit { get; set; }

        public class Detail_
        {
            [JsonPropertyName("accesskey_ration")]
            public long? AccessKeyRation { get; set; }

            [JsonPropertyName("accesskey_tasks_queued")]
            public long? AccessKeyTasksQueued { get; set; }

            [JsonPropertyName("bucket_ration")]
            public long? BucketRation { get; set; }

            [JsonPropertyName("bucket_tasks_queued")]
            public long? BucketTasksQueued { get; set; }

            [JsonPropertyName("limit_reason")]
            public string? LimitReason { get; set; }

            [JsonPropertyName("rationing_engaged")]
            public long? RationingEngaged { get; set; }

            [JsonPropertyName("rationing_level")]
            public long? RationingLevel { get; set; }

            [JsonPropertyName("total_global_limit")]
            public long? TotalGlobalLimit { get; set; }

            [JsonPropertyName("total_tasks_queued")]
            public long? TotalTasksQueued { get; set; }
        }

        public Detail_? Detail { get; set; }
    }

    public async Task<UseLimitResponse> GetUseLimitAsync(string bucket = "")
    {
        var query = new Dictionary<string, string>
        {
            { "check_limit", "1" },
            { "accesskey", _client.AccessKey },
            { "bucket", bucket }
        };

        return await _client.GetAsync<UseLimitResponse>(Url, query);
    }
}