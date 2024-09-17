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

#pragma warning disable CS0612 // Type or member is obsolete
        try
        {
            var response3 = await _client.Wayback.SearchAsync(
                new Wayback.SearchRequest
                {
                    Url = "www.experimentaltvcenter.org",
                    Limit = 25,
                    Offset = 10
                }
            );

            Assert.AreEqual(0, response3.Results.Select(x => x.Timestamp).Except(response2.Results.Select(x => x.Timestamp)).Count());
        }
        catch (InternetArchiveException ex)
        {
            Assert.IsTrue(ex.Message.Contains("no longer supported"));
        }
#pragma warning restore CS0612
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
}
