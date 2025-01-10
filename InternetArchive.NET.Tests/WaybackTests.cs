using System.Xml.Linq;

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

#pragma warning disable CS0618 // Type or member is obsolete
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
#pragma warning restore CS0618
    }

    [TestMethod]
    public async Task SearchAsyncRange()
    {
        var startTime = new DateTime(2000, 1, 1);
        var endTime = new DateTime(2002, 1, 1);

        var request = new Wayback.SearchRequest
        {
            Url = "www.experimentaltvcenter.org",
            StartTime = startTime,
            EndTime = endTime
        };

        var response = await _client.Wayback.SearchAsync(request);

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

    private static async Task<string> GetRandomUrlAsync(string sitemapUrl)
    {
        var sitemap = await _httpClient.GetStringAsync(sitemapUrl);
        var sitemapXml = XDocument.Parse(sitemap);

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var urls = sitemapXml.Descendants(ns + "loc").Select(loc => loc.Value).ToList();
        if (urls.Count == 0) throw new Exception($"No URLs found in the sitemap {sitemapUrl}");

        var index = _random.Next(urls.Count);
        return urls[index] ?? throw new Exception("URL is null");
    }

    [TestMethod]
    public async Task SavePageAsync()
    {
        var url = await GetRandomUrlAsync("https://www.videohistoryproject.org/sitemap.xml");

        var request = new Wayback.SavePageRequest
        {
            Url = url,
            JavascriptTimeout = 0
        };

        var response = await _client.Wayback.SavePageAsync(request);

        Assert.IsNotNull(response);
        Assert.IsNotNull(response.Url);
        Assert.IsNotNull(response.JobId);

        var status = await _client.Wayback.GetSavePageStatusAsync(response.JobId);

        Assert.IsNotNull(status);
    }

    [TestMethod]
    public async Task SavePageGetSystemStatusAsync()
    {
        var response = await _client.Wayback.GetSavePageSystemStatusAsync();

        Assert.IsNotNull(response);
        Assert.IsNotNull(response.Status);
        Assert.IsNotNull(response.RecentCaptures);
    }
}
