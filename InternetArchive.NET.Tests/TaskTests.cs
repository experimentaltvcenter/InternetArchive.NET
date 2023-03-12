namespace InternetArchiveTests;

[TestClass]
public class TaskTests
{
    [TestMethod]
    public async Task GetTasksAsync()
    {
        var request = new Tasks.GetRequest { Submitter = _config.EmailAddress };
        var response = await _client.Tasks.GetAsync(request);

        Assert.IsTrue(response.Success);
    }

    [TestMethod]
    public async Task GetItemTasksAsync()
    {
        string identifier = await GetSharedTestIdentifierAsync();

        var request = new Tasks.GetRequest { Identifier = identifier, Catalog = true, History = true};
        var response = await _client.Tasks.GetAsync(request);

        Assert.IsNotNull(response);
        Assert.IsTrue(response.Success);
        Assert.IsNull(response.Cursor);

        Assert.IsNotNull(response.Value);
        Assert.IsNotNull(response.Value.Summary);
        Assert.IsNotNull(response.Value.Summary.Error);
        Assert.IsNotNull(response.Value.Summary.Paused);
        Assert.IsNotNull(response.Value.Summary.Queued);
        Assert.IsNotNull(response.Value.Summary.Running);

        Assert.IsNotNull(response.Value.History);
        var history = response.Value.History.First();

        Assert.IsNotNull(history.Args);
        Assert.IsNotNull(history.Command);
        Assert.IsNotNull(history.DateSubmitted);
        Assert.IsNotNull(history.Finished);
        Assert.AreEqual(identifier, history.Identifier);
        Assert.IsNotNull(history.Priority);
        Assert.IsNotNull(history.Server);
        Assert.IsNotNull(history.Submitter);
        Assert.IsNotNull(history.TaskId);
    }

    private static void ValidateSubmitResponse(Tasks.SubmitResponse? response)
    {
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Success);
        Assert.IsNotNull(response.Value);
        Assert.IsNotNull(response.Value.TaskId);
        Assert.IsNotNull(response.Value.Log);
    }

    [TestMethod]
    public async Task DarkUndarkItemAsync()
    {
        string identifier = await CreateTestItemAsync();

        var response = await _client.Tasks.MakeDarkAsync(identifier, "test item - please delete");
        ValidateSubmitResponse(response);

        response = await _client.Tasks.MakeUndarkAsync(identifier, "test item - please delete");
        ValidateSubmitResponse(response);
    }
}
