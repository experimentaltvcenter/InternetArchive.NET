namespace InternetArchiveTests;

[TestClass]
public class SearchTests
{
    [TestMethod]
    public async Task ScrapeAsync()
    {
        var request = new Search.ScrapeRequest
        {
            Query = "scanimate",
            Fields = new[] { "identifier", "title", "description" },
            Sorts = new[] { "title" }
        };
        
        var response = await _client.Search.ScrapeAsync(request);
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.Count);
        Assert.IsNotNull(response.Total);

        Assert.AreEqual(response.Count, response.Total);
        Assert.IsNull(response.Cursor);

        Assert.AreEqual(response.Count, response.Items.Count());

        using var json = await _client.Search.ScrapeAsJsonAsync(request);
        var count = json.RootElement.GetProperty("count").GetInt32();
        Assert.AreEqual(response.Count, count);
    }
}
