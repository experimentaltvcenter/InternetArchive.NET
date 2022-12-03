namespace InternetArchiveTests;

[TestClass]
public class WaybackTests
{
    [TestMethod]
    public async Task IsAvailable()
    {
        var response = await _client.Wayback.IsAvailable("www.bombfactory.com");

        Assert.IsNotNull(response);
        Assert.IsTrue(response.IsAvailable);
        Assert.IsNotNull(response.Url);
        Assert.IsNotNull(response.Timestamp);
        Assert.AreEqual(200, response.Status);
    }

    [TestMethod]
    public async Task IsAvailableTimestamp()
    {
        var response = await _client.Wayback.IsAvailable("www.bombfactory.com", new DateTime(2000, 7, 4));

        Assert.IsNotNull(response);
        Assert.IsTrue(response.IsAvailable);
        Assert.IsNotNull(response.Url);
        Assert.AreEqual(2000, response.Timestamp?.Year);
        Assert.AreEqual(200, response.Status);
    }

    [TestMethod]
    public async Task IsNotAvailable()
    {
        var response = await _client.Wayback.IsAvailable("www.bombfactory.com__");

        Assert.IsNotNull(response);
        Assert.IsFalse(response.IsAvailable);
    }
}