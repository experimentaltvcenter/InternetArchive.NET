namespace InternetArchiveTests;

[TestClass]
public class ChangeTests
{
    private readonly int _pageSize = 50000;

    private static string ValidateResponse(Changes.GetResponse response, int? countExpected = null)
    {
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.Token);

        Assert.IsNotNull(response.Changes);

        if (countExpected != null)
        {
            Assert.AreEqual(countExpected, response.Changes.Count());
        }

        return response.Token;
    }

    [TestMethod]
    public async Task GetAsync()
    {
        var response = await _client.Changes.GetAsync(new DateTime(2021, 01, 01));
        string token = ValidateResponse(response, _pageSize);

        response = await _client.Changes.GetAsync(token);
        ValidateResponse(response, _pageSize);

        response = await _client.Changes.GetAsync(new DateOnly(2021, 01, 01));
        token = ValidateResponse(response, _pageSize);

        response = await _client.Changes.GetAsync(token);
        ValidateResponse(response, _pageSize);
    }

    [TestMethod]
    public async Task GetFromBeginningAsync()
    {
        var response = await _client.Changes.GetFromBeginningAsync();
        string token = ValidateResponse(response, _pageSize);

        response = await _client.Changes.GetAsync(token);
        ValidateResponse(response, _pageSize);
    }

    [TestMethod]
    public async Task GetStartingNowAsync()
    {
        var response = await _client.Changes.GetStartingNowAsync();
        string token = ValidateResponse(response, 0);

        response = await _client.Changes.GetAsync(token);
        ValidateResponse(response);
    }
}