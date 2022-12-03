using System.Text.Json.Serialization;

namespace InternetArchiveTests;

[TestClass]
public class JsonConverterTests
{
    [TestMethod]
    public void EnumerableStringNullableConverter()
    {
        var response = new Metadata.ReadResponse();
        var json = JsonSerializer.Serialize(response);

        using var test = JsonSerializer.Deserialize<Metadata.ReadResponse>(json);

        Assert.IsNotNull(test);
        Assert.IsNull(test.WorkableServers);

        using var test2 = JsonSerializer.Deserialize<Metadata.ReadResponse>
        (
            "{\"workable_servers\":\"1\"}"
        );

        Assert.AreEqual(1, test2?.WorkableServers?.Count());

        response.WorkableServers = new[] { "1", "2" };

        json = JsonSerializer.Serialize(response);
        using var test3 = JsonSerializer.Deserialize<Metadata.ReadResponse>(json);

        Assert.AreEqual(2, test3?.WorkableServers?.Count());
    }

    public class TestDateOnly
    {
        public DateOnly? TestDate { get; set; }
    }

    [TestMethod]
    public void DateOnlyConverter()
    {
        var testDate = new DateOnly(2001, 01, 25);

        var response = new TestDateOnly { TestDate = testDate };
        var json = JsonSerializer.Serialize(response);

        var test = JsonSerializer.Deserialize<TestDateOnly>(json);
        Assert.IsNotNull(test);
        Assert.AreEqual(testDate, test.TestDate);

        json = "{ \"TestDate\" : null }";
        test = JsonSerializer.Deserialize<TestDateOnly>(json);
        Assert.IsNotNull(test);
        Assert.IsNull(test.TestDate);
    }
}