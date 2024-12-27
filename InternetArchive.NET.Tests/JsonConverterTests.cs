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

        var test = JsonSerializer.Deserialize<Metadata.ReadResponse>(json);

        Assert.IsNotNull(test);
        Assert.IsNull(test.WorkableServers);

        var test2 = JsonSerializer.Deserialize<Metadata.ReadResponse>
        (
            "{\"workable_servers\":\"1\"}"
        );

        Assert.AreEqual(1, test2?.WorkableServers?.Count());

        response.WorkableServers = [ "1", "2" ];

        json = JsonSerializer.Serialize(response);
        var test3 = JsonSerializer.Deserialize<Metadata.ReadResponse>(json);

        Assert.AreEqual(2, test3?.WorkableServers?.Count());
    }

    private class TestUnixEpoch
    {
        [JsonConverter(typeof(UnixEpochDateTimeNullableConverter))]
        public DateTimeOffset? TestDate { get; set; }
    }

    [TestMethod]
    public void UnixEpochDateTimeNullableConverter()
    {
        var testDate = new DateTimeOffset(2001, 01, 25, 0, 0, 0, TimeSpan.Zero);

        var response = new TestUnixEpoch { TestDate = testDate };
        var json = JsonSerializer.Serialize(response);

        var test = JsonSerializer.Deserialize<TestUnixEpoch>(json);
        Assert.IsNotNull(test);
        Assert.AreEqual(testDate, test.TestDate);

        json = $"{{ \"TestDate\" : \"{testDate.ToUnixTimeSeconds()}\" }}";
        test = JsonSerializer.Deserialize<TestUnixEpoch>(json);
        Assert.IsNotNull(test);
        Assert.AreEqual(testDate, test.TestDate);

        json = "{ \"TestDate\" : null }";
        test = JsonSerializer.Deserialize<TestUnixEpoch>(json);
        Assert.IsNotNull(test);
        Assert.IsNull(test.TestDate);
    }

    private class TestDateOnly
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
