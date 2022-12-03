using System.Text;

namespace InternetArchiveTests;

[TestClass]
public class ItemTests
{
    private static string _largeTextPath = null!;

    [ClassInitialize()]
    public static void ClassInit(TestContext _)
    {
        var chars = Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz          ");
        var s = new byte[1024 * 1024 * 11]; // 11MB

        var random = new Random();
        for (int i = 0; i < s.Length; i++)
        {
            s[i] = chars[random.Next(chars.Length)];
        }

        _largeTextPath = Path.Combine(Path.GetTempPath(), "large.txt");
        using var stream = File.Create(_largeTextPath);
        stream.Write(s, 0, s.Length);
    }

    [TestMethod]
    public async Task GetUseLimitAsync()
    {
        var response = await _client.Item.GetUseLimitAsync(_config.TestItem);

        Assert.IsNotNull(response);

        Assert.AreEqual(_config.TestItem, response.Bucket);
        Assert.AreEqual(0, response.OverLimit);

        Assert.IsNotNull(response.Detail);
        Assert.AreEqual("", response.Detail.LimitReason);

        Assert.IsNotNull(response.Detail.AccessKeyRation);
        Assert.IsNotNull(response.Detail.AccessKeyTasksQueued);
        Assert.IsNotNull(response.Detail.BucketRation);
        Assert.IsNotNull(response.Detail.BucketTasksQueued);
        Assert.IsNotNull(response.Detail.RationingEngaged);
        Assert.IsNotNull(response.Detail.RationingLevel);
        Assert.IsNotNull(response.Detail.TotalGlobalLimit);
        Assert.IsNotNull(response.Detail.TotalTasksQueued);
    }

    private static void AssertHasMetadata(Metadata.ReadResponse response, string key, string expectedValue)
    {
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.Metadata);
        Assert.IsTrue(response.Metadata.RootElement.TryGetProperty(key, out var element));
        Assert.AreEqual(expectedValue, element.GetString());
    }

    private static void AssertNoMetadata(Metadata.ReadResponse response, string key)
    {
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.Metadata);
        Assert.IsFalse(response.Metadata.RootElement.TryGetProperty(key, out var element));
    }

    [TestMethod]
    public async Task CreateModifyDeleteAsync()
    {
        const string _remoteFilename2 = "hello; again #2.txt";

        var extraMetadata = new List<KeyValuePair<string, object?>>
        {
            new KeyValuePair<string, object?>("title", "test_title"),
            new KeyValuePair<string, object?>("testfield", "hello")
        };

        var identifier = await CreateTestItemAsync(extraMetadata: extraMetadata);

        await WaitForServerAsync(identifier);

        // verify metadata

        using var response1 = await _client.Metadata.ReadAsync(identifier);
        AssertHasMetadata(response1, "title", "test_title");
        AssertHasMetadata(response1, "testfield", "hello");

        Assert.IsNotNull(response1.Metadata);
        var metadataFiltered = response1.Metadata.RootElement.EnumerateObject().Where(x => x.Name != "title" && x.Name != "testfield" && x.Name != "collection").Select(x => new KeyValuePair<string, object?>(x.Name, x.Value)).ToList();
        metadataFiltered.Add(new KeyValuePair<string, object?>("collection", "test_collection"));

        // delete existing metadata

        await _client.Item.PutAsync(new Item.PutRequest
        {
            Bucket = identifier,
            Metadata = metadataFiltered,
            DeleteExistingMetadata = true
        });

        await WaitForServerAsync(identifier);

        using var response2 = await _client.Metadata.ReadAsync(identifier);
        Assert.IsNotNull(response2);
        AssertHasMetadata(response2, "title", identifier); // title reverts to identifier/bucket when removed
        AssertNoMetadata(response2, "testfield");

        // add another file

        await _client.Item.PutAsync(new Item.PutRequest
        {
            Bucket = identifier,
            LocalPath = _config.LocalFilename,
            RemoteFilename = _remoteFilename2,
            NoDerive = true
        });

        await WaitForServerAsync(identifier);

        using var response3 = await _client.Metadata.ReadAsync(identifier);
        Assert.IsNotNull(response3?.Files.Where(x => x.Name == _config.RemoteFilename).SingleOrDefault());
        Assert.IsNotNull(response3?.Files.Where(x => x.Name == _remoteFilename2).SingleOrDefault());

        // delete file

        await _client.Item.DeleteAsync(new Item.DeleteRequest
        {
             Bucket = identifier,
             RemoteFilename = _remoteFilename2,
             CascadeDelete = true,
             KeepOldVersion = false             
        });

        await WaitForServerAsync(identifier);

        using var response4 = await _client.Metadata.ReadAsync(identifier);
        Assert.IsNotNull(response4?.Files.Where(x => x.Name == _config.RemoteFilename).SingleOrDefault());
        Assert.IsNull(response4?.Files.Where(x => x.Name == _remoteFilename2).SingleOrDefault());

        // delete other file

        await _client.Item.DeleteAsync(new Item.DeleteRequest
        {
            Bucket = identifier,
            RemoteFilename = _config.RemoteFilename,
            CascadeDelete = true,
            KeepOldVersion = false
        });

        await WaitForServerAsync(identifier);

        using var response5 = await _client.Metadata.ReadAsync(identifier);
        Assert.IsNull(response5?.Files.Where(x => x.Name == _config.RemoteFilename).SingleOrDefault());
        Assert.IsNull(response5?.Files.Where(x => x.Name == _remoteFilename2).SingleOrDefault());

        if (_config.CanDelete)
        {
            await _client.Tasks.SubmitAsync(identifier, Tasks.Command.Delete);
            await WaitForServerAsync(identifier);
        }
    }

    private static Item.PutRequest CreateMultipartRequest(string identifier, bool createBucket = true)
    {
        var metadata = new List<KeyValuePair<string, object?>>
        {
            new KeyValuePair<string, object?>("collection", "test_collection"),
            new KeyValuePair<string, object?>("mediatype", "texts"),
            new KeyValuePair<string, object?>("noindex", "true"),
        };

        return new Item.PutRequest
        {
            Bucket = identifier,
            LocalPath = _largeTextPath,
            Metadata = metadata,
            CreateBucket = createBucket,
            NoDerive = true,
            MultipartUploadMinimumSize = 0, // force multipart upload
            MultipartUploadChunkSize = 1024 * 1024 * 5 // 5 MB chunks
        };
    }

    [TestMethod]
    public async Task AbortUpload()
    {
        string identifier = _config.TestItem;
        var putRequest = CreateMultipartRequest(identifier);

        await _client.Item.AbortUploadAsync(putRequest);
        await WaitForServerAsync(identifier);
    }

    [TestMethod]
    public async Task AbortAllBucketUploads()
    {
        string identifier = _config.TestItem;

        await _client.Item.AbortUploadAsync(identifier);
        await WaitForServerAsync(identifier);
    }

    [TestMethod]
    public async Task StartThenAbortUpload()
    {
        string identifier = $"etc-tmp-{Guid.NewGuid():N}";
        var putRequest = CreateMultipartRequest(identifier);

        putRequest.MultipartUploadSkipParts = new[] { 1, 2 };

        await _client.Item.PutAsync(putRequest);
        await WaitForServerAsync(identifier);

        await _client.Item.AbortUploadAsync(putRequest);
    }

    [TestMethod]
    public async Task UploadMultipart()
    {
        string identifier = $"etc-tmp-{Guid.NewGuid():N}";
        await _client.Item.PutAsync(CreateMultipartRequest(identifier));
    }

    [TestMethod]
    public async Task UploadMultipartExistingWithContinue()
    {
        string identifier = _config.TestItem;
        var putRequest = CreateMultipartRequest(identifier, createBucket: false);
        putRequest.MultipartUploadSkipParts = new[] { 1, 2 };

        await _client.Item.PutAsync(putRequest);

        putRequest.MultipartUploadSkipParts = new List<int>();
        await _client.Item.PutAsync(putRequest);

        await WaitForServerAsync(identifier);
    }
}
