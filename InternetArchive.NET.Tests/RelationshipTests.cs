namespace InternetArchiveTests;

[TestClass]
public class RelationshipTests
{
    [TestMethod]
    public async Task AddRemoveListAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.TestCollection))
        {
            Assert.Inconclusive("Skipping test because _config.TestCollection is null");
        }

        string identifier = await GetSharedTestIdentifierAsync();

        var response = await _client.Relationships.AddAsync(identifier, _config.TestCollection, _config.TestList);
        Assert.IsTrue(response?.Success);

        var parents = await _client.Relationships.GetParentsAsync(identifier);
        Assert.IsTrue(parents.Lists.ContainsKey(_config.TestCollection), "Item not added to collection");

        var children = await _client.Relationships.GetChildrenAsync(_config.TestCollection, _config.TestList);
        Assert.IsNotNull(children);
        // can't test this because query API is not in immediate sync with metadata API and there is no identifier to wait on
        // await WaitForServerAsync(_config.TestCollection)

        response = await _client.Relationships.RemoveAsync(identifier, _config.TestCollection, _config.TestList);
        Assert.IsTrue(response?.Success);

        parents = await _client.Relationships.GetParentsAsync(identifier);
        Assert.IsNotNull(parents?.Error); // no parent so returns error string
        Assert.IsFalse(parents.Lists.ContainsKey(_config.TestCollection), "Item not removed from collection");
    }

    [TestMethod]
    public async Task ParentChildAsync()
    {
        var children = await _client.Relationships.GetChildrenAsync(_config.TestParent, rows: 1);

        string? testChild = children.Identifiers().FirstOrDefault();
        Assert.IsNotNull(testChild);

        var parents = await _client.Relationships.GetParentsAsync(testChild);
        Assert.IsTrue(parents.Lists.ContainsKey(_config.TestParent));
    }

    [TestMethod]
    public async Task GetChildrenAsync()
    {
        var children = await _client.Relationships.GetChildrenAsync(_config.TestParent, rows: 7);

        Assert.IsNotNull(children);
        Assert.IsNotNull(children.Response);
        Assert.IsNotNull(children.Response.Docs);
        Assert.IsNotNull(children.Response.NumFound);
        Assert.IsNotNull(children.Response.Start);

        Assert.AreEqual(7, children.Response.Docs.Count());
    }

    [TestMethod]
    public async Task GetParentsAsync()
    {
        var parents = await _client.Relationships.GetParentsAsync(_config.TestChild);

        Assert.IsNotNull(parents);
        Assert.IsNotNull(parents.Lists);

        var list = parents.Lists.Single();
        Assert.AreEqual(_config.TestParent, list.Key);

        Assert.IsNotNull(list.Value);
        Assert.IsNotNull(list.Value.LastChangedBy);
        Assert.IsNotNull(list.Value.LastChangedDate);

        Assert.IsNotNull(list.Value.Notes);
    }
}