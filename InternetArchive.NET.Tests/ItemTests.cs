namespace InternetArchiveTests;

[TestClass]
public class ItemTests
{
    [TestMethod]
    public async Task GetUseLimitAsync()
    {
        var response = await _client.Item.GetUseLimitAsync(_config.ReadOnlyItem);

        Assert.IsNotNull(response);

        Assert.AreEqual(_config.ReadOnlyItem, response.Bucket);
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
        const string _remoteFilename2 = "hello-again.txt";

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
             Bucket = $"{identifier}/{_remoteFilename2}",
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
            Bucket = $"{identifier}/{_config.RemoteFilename}",
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
}
