using System.Runtime.Serialization;
using System.Xml.Serialization;

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
        public Stream? SourceStream { get; set; }

        public string? RemoteFilename { get; set; }
        public IEnumerable<KeyValuePair<string, object?>> Metadata { get; set; } = Enumerable.Empty<KeyValuePair<string, object?>>();

        public bool CreateBucket { get; set; }
        public bool NoDerive { get; set; }
        public bool KeepOldVersion { get; set; }
        public bool DeleteExistingMetadata { get; set; }

        public long MultipartUploadMinimumSize { get; set; } = 1024 * 1024 * 300; // use multipart for files over 300 MB
        public int MultipartUploadChunkSize { get; set; } = 1024 * 1024 * 200; // upload in 200 MB chunks
        public int MultipartUploadThreadCount { get; set; } = 3; // three simultaneous uploads
        internal IEnumerable<int> MultipartUploadSkipParts { get; set; } = Enumerable.Empty<int>(); // for testing

        public string? SimulateError { get; set; }

        internal bool HasFilename()
        {
            return RemoteFilename != null || LocalPath != null;
        }

        internal string Filename(bool encoded = true)
        {
            var filename = RemoteFilename ?? Path.GetFileName(LocalPath) ?? throw new Exception("RemoteFilename or LocalPath required");
            return encoded ? Encode(filename) : filename;
        }
    }

    private static string Encode(string s)
    {
        // UrlEncode replaces spaces so we use this instead:
        return s.Replace(";", "%3b").Replace("#", "%23");
    }

    public async Task<HttpResponseMessage?> PutAsync(PutRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Bucket == null) throw new Exception("A Bucket identifier is required");
        if (request.SourceStream?.CanSeek == false) throw new Exception("SourceStream must be seekable");

        Stream? sourceStream = null;

        try
        {
            using var uploadRequest = new HttpRequestMessage();
            bool isMultipartUpload = false;

            if (request.HasFilename())
            {
                if (request.SourceStream == null && request.LocalPath == null) throw new Exception("A SourceStream or LocalPath is required");

                sourceStream = request.SourceStream ?? File.OpenRead(request.LocalPath!);
                if (sourceStream.Length >= request.MultipartUploadMinimumSize) isMultipartUpload = true;

                uploadRequest.RequestUri = new Uri($"{Url}/{request.Bucket}/{request.Filename()}{(isMultipartUpload ? "?uploads" : null)}");
                uploadRequest.Headers.Add($"x-archive-size-hint", $"{sourceStream.Length}");

                if (isMultipartUpload == false)
                {
                    uploadRequest.Content = new StreamContent(sourceStream);

                    using var md5 = MD5.Create();
                    uploadRequest.Content.Headers.ContentMD5 = md5.ComputeHash(sourceStream);
                    sourceStream.Seek(0, SeekOrigin.Begin);
                }
            }
            else
            {
                uploadRequest.RequestUri = new Uri($"{Url}/{request.Bucket}");
            }

            AddMetadata(uploadRequest, request.Metadata);

            if (request.CreateBucket == true) uploadRequest.Headers.Add("x-archive-auto-make-bucket", "1");
            if (request.KeepOldVersion == true) uploadRequest.Headers.Add("x-archive-keep-old-version", "1");
            if (request.NoDerive == true) uploadRequest.Headers.Add("x-archive-queue-derive", "0");
            if (request.DeleteExistingMetadata == true) uploadRequest.Headers.Add("x-archive-ignore-preexisting-bucket", "1");
            if (request.SimulateError != null) uploadRequest.Headers.Add("x-archive-simulate-error", request.SimulateError);

            if (isMultipartUpload == false)
            {
                uploadRequest.Method = HttpMethod.Put;
                return await _client.SendAsync<HttpResponseMessage>(uploadRequest, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await MultipartUpload(request, sourceStream!, uploadRequest, cancellationToken).ConfigureAwait(false);
            }

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
        }
        finally
        {
            if (request.SourceStream == null) sourceStream?.Dispose();
        }
    }

    private async Task<IEnumerable<XmlModels.Upload>> GetUploadsInProgressAsync(string bucket, CancellationToken cancellationToken)
    {
        return await GetUploadsInProgressAsync(new PutRequest { Bucket = bucket }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IEnumerable<XmlModels.Upload>> GetUploadsInProgressAsync(PutRequest request, CancellationToken cancellationToken)
    {
        var listMultipartUploadsRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{Url}/{request.Bucket}/?uploads")
        };

        var listMultipartUploadsResult = await _client.SendAsync<XmlModels.ListMultipartUploadsResult>(listMultipartUploadsRequest, cancellationToken).ConfigureAwait(false);

        if (request.HasFilename())
        {
            return listMultipartUploadsResult?.Uploads.Where(x => x.Key == request.Filename(encoded: false)) ?? Enumerable.Empty<XmlModels.Upload>();
        }
        else
        {
            return listMultipartUploadsResult?.Uploads ?? Enumerable.Empty<XmlModels.Upload>();
        }
    }

    public async Task AbortUploadAsync(string bucket, CancellationToken cancellationToken = default)
    {
        var uploads = await GetUploadsInProgressAsync(bucket, cancellationToken).ConfigureAwait(false);
        foreach (var upload in uploads)
        {
            var abortMultipartUploadRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"{Url}/{bucket}/{upload.Key}?uploadId={upload.UploadId}")
            };

            await _client.SendAsync<HttpResponseMessage?>(abortMultipartUploadRequest, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task AbortUploadAsync(PutRequest request, CancellationToken cancellationToken = default)
    {
        var uploads = await GetUploadsInProgressAsync(request, cancellationToken).ConfigureAwait(false);
        foreach (var upload in uploads)
        {
            var abortMultipartUploadRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"{Url}/{request.Bucket}/{request.Filename()}?uploadId={upload.UploadId}")
            };

            await _client.SendAsync<HttpResponseMessage?>(abortMultipartUploadRequest, cancellationToken).ConfigureAwait(false);
        }
    }

    public class XmlModels
    {
        [XmlRoot(Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public class ListMultipartUploadsResult
        {
            public string Bucket { get; set; } = null!;
            public string KeyMarker { get; set; } = null!;
            public string UploadIdMarker { get; set; } = null!;
            public string NextKeyMarker { get; set; } = null!;
            public string NextUploadIdMarker { get; set; } = null!;
            public int MaxUploads { get; set; }
            public bool IsTruncated { get; set; }

            [XmlElement("Upload")]
            public List<Upload> Uploads { get; set; } = null!;
        }

        public class Upload
        {
            public string Key { get; set; } = null!;
            public string UploadId { get; set; } = null!;
        }

        [XmlRoot(Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public class InitiateMultipartUploadResult
        {
            public string Bucket { get; set; } = null!;
            public string Key { get; set; } = null!;
            public string UploadId { get; set; } = null!;
        }

        [XmlRoot(Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public class ListPartsResult
        {
            public string Bucket { get; set; } = null!;
            public string Key { get; set; } = null!;
            public string UploadId { get; set; } = null!;
            public string PartNumberMarker { get; set; } = null!;
            public string NextPartNumberMarker { get; set; } = null!;
            public int MaxParts { get; set; }
            public bool IsTruncated { get; set; }

            [XmlElement("Part")]
            public List<Part> Parts { get; set; } = null!;
        }

        [DataContract(Name = "Part", Namespace = "")]
        public class Part
        {
            [DataMember]
            public int PartNumber { get; set; }
            [DataMember]
            public string ETag { get; set; } = null!;
        }
    }

    private async Task<HttpResponseMessage?> MultipartUpload(PutRequest request, Stream sourceStream, HttpRequestMessage uploadRequest, CancellationToken cancellationToken)
    {
        var parts = new ConcurrentBag<XmlModels.Part>();
        string uploadId = null!;
        using var synchronizedStream = Stream.Synchronized(sourceStream);

        try
        {
            // see if there's already a multipart upload in progress

            var uploads = await GetUploadsInProgressAsync(request, cancellationToken).ConfigureAwait(false);
            if (uploads.Any())
            {
                uploadId = uploads.First().UploadId;

                var listPartsRequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{Url}/{request.Bucket}/{request.Filename()}?uploadId={uploadId}")
                };

                var listPartsResult = await _client.SendAsync<XmlModels.ListPartsResult>(listPartsRequest, cancellationToken).ConfigureAwait(false);
                foreach (var part in listPartsResult!.Parts) parts.Add(part);
            }
        }
        catch (InternetArchiveRequestException ex)
        {
            if (ex.StatusCode != HttpStatusCode.NotFound) throw;
        }

        if (uploadId == null)
        {
            uploadRequest.Method = HttpMethod.Post;

            var initiateMultipartUploadResult = await _client.SendAsync<XmlModels.InitiateMultipartUploadResult>(uploadRequest, cancellationToken).ConfigureAwait(false);
            if (initiateMultipartUploadResult == null) return null; // dry run

            uploadId = initiateMultipartUploadResult.UploadId;
        }

        var totalParts = synchronizedStream.Length / request.MultipartUploadChunkSize;
        if (synchronizedStream.Length % request.MultipartUploadChunkSize != 0) totalParts++;

        using var semaphore = new SemaphoreSlim(request.MultipartUploadThreadCount);
        var tasks = new List<Task>();

        for (var i = 1; i <= totalParts; i++)
        {
            if (request.MultipartUploadSkipParts.Contains(i) || parts.Any(x=> x.PartNumber == i)) continue;

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            var partNumber = i;

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var buffer = new byte[request.MultipartUploadChunkSize];
                    int length;

                    synchronizedStream.Seek((partNumber - 1) * request.MultipartUploadChunkSize, SeekOrigin.Begin);
                    length = synchronizedStream.Read(buffer, 0, request.MultipartUploadChunkSize);

                    using var partRequest = new HttpRequestMessage
                    {
                        Method = HttpMethod.Put,
                        RequestUri = new Uri($"{Url}/{request.Bucket}/{request.Filename()}?partNumber={partNumber}&uploadId={uploadId}"),
                        Content = new ByteArrayContent(buffer, 0, length)
                    };

                    using var md5 = MD5.Create();
                    partRequest.Content.Headers.ContentMD5 = md5.ComputeHash(buffer, 0, length);

                    var partResponse = await _client.SendAsync<HttpResponseMessage>(partRequest, cancellationToken).ConfigureAwait(false);
                    if (partResponse?.Headers?.ETag?.Tag == null) throw new Exception("Invalid multipart upload response for part {partNumber}");

                    parts.Add(new XmlModels.Part
                    {
                        PartNumber = partNumber,
                        ETag = partResponse.Headers.ETag.Tag
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        if (parts.Count != totalParts) return null;

        var serializer = new DataContractSerializer(typeof(IEnumerable<XmlModels.Part>), "CompleteMultipartUpload", "http://s3.amazonaws.com/doc/2006-03-01/");
        using var ms = new MemoryStream();
        serializer.WriteObject(ms, parts.OrderBy(x => x.PartNumber));
        string xml = Encoding.UTF8.GetString(ms.ToArray());

        using var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Url}/{request.Bucket}/{request.Filename()}?uploadId={uploadId}"),
            Content = new StringContent(xml, Encoding.UTF8, "application/xml"),
        };

        return await _client.SendAsync<HttpResponseMessage>(httpRequest, cancellationToken).ConfigureAwait(false);
    }

    public class DeleteRequest
    {
        public string? Bucket { get; set; }
        public string? RemoteFilename { get; set; }
        public bool KeepOldVersion { get; set; }
        public bool CascadeDelete { get; set; }
    }

    public async Task<HttpResponseMessage?> DeleteAsync(DeleteRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Bucket == null) throw new Exception("identifier required");
        if (request.Bucket.Contains('/')) throw new Exception("slash not allowed in bucket name; use .RemoteFilename to specify the file to delete in a bucket");

        string requestUri = $"{Url}/{request.Bucket}";
        if (request.RemoteFilename != null) requestUri += $"/{Encode(request.RemoteFilename)}";

        using var httpRequest = new HttpRequestMessage
        {
            RequestUri = new Uri(requestUri),
            Method = HttpMethod.Delete
        };

        if (request.KeepOldVersion == true) httpRequest.Headers.Add("x-archive-keep-old-version", "1");
        if (request.CascadeDelete == true) httpRequest.Headers.Add("x-archive-cascade-delete", "1");

        return await _client.SendAsync<HttpResponseMessage>(httpRequest, cancellationToken).ConfigureAwait(false);
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

    public async Task<UseLimitResponse> GetUseLimitAsync(string bucket = "", CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string>
        {
            { "check_limit", "1" },
            { "accesskey", _client.AccessKey },
            { "bucket", bucket }
        };

        return await _client.GetAsync<UseLimitResponse>(Url, query, cancellationToken).ConfigureAwait(false);
    }
}
