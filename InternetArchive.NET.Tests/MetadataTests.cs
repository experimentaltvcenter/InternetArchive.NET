namespace InternetArchiveTests;

[TestClass]
public class MetadataTests
{
    [TestMethod]
    public async Task ReadMetadataAsync()
    {
        using var response = await _client.Metadata.ReadAsync(_config.ReadOnlyItem);
        
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.DataNodePrimary);
        Assert.IsNotNull(response.DataNodeSecondary);
        Assert.IsNull(response.DataNodeSolo);
        Assert.IsNotNull(response.DateCreated);
        Assert.IsNotNull(response.DateLastUpdated);
        Assert.IsNotNull(response.Dir);
        Assert.IsNotNull(response.Files);
        Assert.IsNotNull(response.Metadata);
        Assert.IsNotNull(response.Size);
        Assert.IsNotNull(response.Uniq);

        Assert.IsNotNull(response.WorkableServers);
        Assert.IsTrue(response.WorkableServers.Any());
        // Assert.IsNull(response.ServersUnavailable); may be null or not

        var collection = response.Metadata.RootElement.EnumerateObject().Where(x => x.NameEquals("collection")).SingleOrDefault();
        Assert.IsNotNull(collection);

        var file = response.Files.Where(x => x.Format == "Text").SingleOrDefault();

        Assert.IsNotNull(file);
        Assert.IsNotNull(file.Crc32);
        Assert.IsNotNull(file.Format);
        Assert.IsNotNull(file.Md5);
        Assert.IsNotNull(file.ModificationDate);
        Assert.IsNotNull(file.Name);
        Assert.IsNotNull(file.Sha1);
        Assert.IsNotNull(file.Size);
        Assert.IsNotNull(file.Source);
        Assert.IsNotNull(file.VirusCheckDate);
    }

    [TestMethod]
    public async Task WriteMetadataAsync()
    {
        using var readResponse1 = await _client.Metadata.ReadAsync(_config.ReadOnlyItem);

        var patch = new JsonPatchDocument();
        string value;

        if (readResponse1?.Metadata?.RootElement.TryGetProperty("testkey", out var element) == true)
        {
            value = element.GetString() == "flop" ? "flip" : "flop";
            patch.Replace("/testkey", value);
        }
        else
        {
            value = "flip";
            patch.Add("/testkey", value);
        }

        var writeResponse = await _client.Metadata.WriteAsync(_config.ReadOnlyItem, patch);
        
        Assert.IsNotNull(writeResponse);
        Assert.IsTrue(writeResponse.Success);
        Assert.IsNull(writeResponse.Error);
        Assert.IsNotNull(writeResponse.Log);
        Assert.IsNotNull(writeResponse.TaskId);

        using var readResponse2 = await _client.Metadata.ReadAsync(_config.ReadOnlyItem);
        Assert.AreEqual(value, readResponse2?.Metadata?.RootElement.GetProperty("testkey").GetString());
    }
}