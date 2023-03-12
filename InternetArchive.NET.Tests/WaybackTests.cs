namespace InternetArchiveTests;

[TestClass]
public class WaybackTests
{
    [TestMethod]
    public async Task SearchAsync()
    {
        var response1 = await _client.Wayback.SearchAsync(
            new Wayback.SearchRequest
            {
                Url = "www.experimentaltvcenter.org",
                Limit = 10
            }
        );

        Assert.IsNotNull(response1);
        Assert.AreEqual(10, response1.Results.Count);
        Assert.IsNotNull(response1.ResumeKey);

        var response2 = await _client.Wayback.SearchAsync(
            new Wayback.SearchRequest
            {
                Url = "www.experimentaltvcenter.org",
                Limit = 25,
                ResumeKey = response1.ResumeKey
            }
        );

        Assert.IsNotNull(response2);
        Assert.AreEqual(25, response2.Results.Count);
        Assert.AreEqual(25, response2.Results.Select(x => x.Timestamp).Except(response1.Results.Select(x => x.Timestamp)).Count());

        var response3 = await _client.Wayback.SearchAsync(
            new Wayback.SearchRequest
            {
                Url = "www.experimentaltvcenter.org",
                Limit = 25,
                Offset = 10
            }
        );

        Assert.AreEqual(0, response3.Results.Select(x=> x.Timestamp).Except(response2.Results.Select(x=> x.Timestamp)).Count());
    }

    [TestMethod]
    public async Task SearchAsyncRange()
    {
        var startTime = new DateTime(2000, 1, 1);
        var endTime = new DateTime(2002, 1, 1);

        var response = await _client.Wayback.SearchAsync(
            new Wayback.SearchRequest
            {
                Url = "www.experimentaltvcenter.org",
                StartTime = startTime,
                EndTime = endTime
            }
        );

        Assert.IsNotNull(response);
        Assert.IsFalse(response.Results.Any(x => x.Timestamp < startTime));
        Assert.IsFalse(response.Results.Any(x => x.Timestamp > endTime));
    }

    [TestMethod]
    public async Task SearchAsyncRangePaging()
    {
        var request = new Wayback.SearchRequest
        {
            Url = "www.experimentaltvcenter.org",
            StartTime = new DateTime(2000, 1, 1),
            EndTime = new DateTime(2002, 1, 1),
            Limit = 3
        };

        while (true)
        {
            var response = await _client.Wayback.SearchAsync(request);
            Assert.IsNotNull(response);

            if (response.ResumeKey == null) break;
            request.ResumeKey = response.ResumeKey;
        }
    }

    [TestMethod]
    public async Task GetNumPagesAsync()
    {
        var pages = await _client.Wayback.GetNumPagesAsync("www.experimentaltvcenter.org");
        Assert.IsNotNull(pages);
    }

    [TestMethod]
    public async Task IsAvailableAsync()
    {
        var response = await _client.Wayback.IsAvailableAsync("www.experimentaltvcenter.org");

        Assert.IsNotNull(response);
        Assert.IsTrue(response.IsAvailable);
        Assert.IsNotNull(response.Url);
        Assert.IsNotNull(response.Timestamp);
        Assert.AreEqual(HttpStatusCode.OK, response.Status);
    }

    [TestMethod]
    public async Task IsAvailableTimestamp()
    {
        var response = await _client.Wayback.IsAvailableAsync("www.bombfactory.com", new DateTime(2000, 7, 4));

        Assert.IsNotNull(response);
        Assert.IsTrue(response.IsAvailable);
        Assert.IsNotNull(response.Url);
        Assert.AreEqual(2000, response.Timestamp?.Year);      
    }

    [TestMethod]
    public async Task IsAvailableTimestampAsync()
    {
        var response = await _client.Wayback.IsAvailableAsync("www.experimentaltvcenter.org", new DateTime(2001, 3, 31));

        Assert.IsNotNull(response);
        Assert.IsTrue(response.IsAvailable);
        Assert.IsNotNull(response.Url);
        Assert.AreEqual(2001, response.Timestamp?.Year);
        Assert.AreEqual(HttpStatusCode.OK, response.Status);
    }

    [TestMethod]
    public async Task IsNotAvailableAsync()
    {
        var response = await _client.Wayback.IsAvailableAsync("www.experimentaltvcenter.org__");

        Assert.IsNotNull(response);
        Assert.IsFalse(response.IsAvailable);
    }
}