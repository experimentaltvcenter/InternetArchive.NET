namespace InternetArchiveTests;

[TestClass]
public class ReviewTests
{
    [TestMethod]
    public async Task ReadUpdateDeleteAsync()
    {
        string identifier = _config.ReadOnlyItem;

        try
        {
            var ignore = await _client.Reviews.GetAsync(identifier);

            // review already exists... clean up from previous test run

            await _client.Reviews.DeleteAsync(identifier);
            await WaitForServerAsync(identifier);
        }
        catch (HttpRequestException ex)
        {
            Assert.AreEqual(HttpStatusCode.NotFound, ex.StatusCode);
        }

        // add a review

        var addRequest = new Reviews.AddOrUpdateRequest
        {
            Title = "Title Text",
            Body = "Body text",
            Identifier = identifier,
            Stars = 3
        };

        var addResponse = await _client.Reviews.AddOrUpdateAsync(addRequest);
        Assert.IsNotNull(addResponse);
        Assert.IsTrue(addResponse.Success);
        Assert.IsNotNull(addResponse.Value);
        Assert.IsNotNull(addResponse.Value.TaskId);
        Assert.IsFalse(addResponse.Value.ReviewUpdated);

        await WaitForServerAsync(identifier);

        var getResponse = await _client.Reviews.GetAsync(identifier);
        Assert.IsTrue(getResponse.Success);
        Assert.IsNotNull(getResponse.Value);
        Assert.AreEqual(addRequest.Title, getResponse.Value.Title);
        Assert.AreEqual(addRequest.Body, getResponse.Value.Body);
        Assert.AreEqual(addRequest.Stars, getResponse.Value.Stars);
        Assert.IsNotNull(getResponse.Value.DateCreated);
        Assert.IsNotNull(getResponse.Value.DateModified);

        // resend same review

        try
        {
            addResponse = await _client.Reviews.AddOrUpdateAsync(addRequest);
            Assert.Fail("Sending same review should not succeed");
        }
        catch (ServerResponseException)
        {
            // ok
        }

        // update review with new title

        addRequest.Title = "New Title Text";
        addResponse = await _client.Reviews.AddOrUpdateAsync(addRequest);
        Assert.IsTrue(addResponse!.Value!.ReviewUpdated);

        await WaitForServerAsync(identifier);

        // verify new title

        getResponse = await _client.Reviews.GetAsync(identifier);
        Assert.IsTrue(getResponse.Success);
        Assert.IsNotNull(getResponse.Value);
        Assert.AreEqual(addRequest.Title, getResponse.Value.Title);

        // delete review

        var deleteResponse = await _client.Reviews.DeleteAsync(identifier);
        Assert.IsNotNull(deleteResponse);
        Assert.IsTrue(deleteResponse.Success);
        Assert.IsNotNull(deleteResponse.Value);
        Assert.IsNotNull(deleteResponse.Value.TaskId);

        await WaitForServerAsync(identifier);

        // verify delete

        try
        {
            var ignore = await _client.Reviews.GetAsync(identifier);
            Assert.Fail("Failed to delete");
        }
        catch (HttpRequestException ex)
        {
            Assert.AreEqual(HttpStatusCode.NotFound, ex.StatusCode);
        }
    }
}
