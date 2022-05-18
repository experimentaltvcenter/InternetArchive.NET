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

        await _client.Relationships.AddAsync(_config.ReadOnlyItem, _config.TestCollection, _config.TestList);
        var parents = await _client.Relationships.GetParentsAsync(_config.ReadOnlyItem);
        Assert.IsTrue(parents.Lists.ContainsKey(_config.TestCollection), "Item not added to collection");

        await _client.Relationships.RemoveAsync(_config.ReadOnlyItem, _config.TestCollection, _config.TestList);
        parents = await _client.Relationships.GetParentsAsync(_config.ReadOnlyItem);
        Assert.IsFalse(parents.Lists.ContainsKey(_config.TestCollection), "Item not removed from collection");
    }

    [TestMethod]
    public async Task ParentChildAsync()
    {
        var children = await _client.Relationships.GetChildrenAsync(_config.TestParent);

        string? testChild = children.Identifiers().FirstOrDefault();
        Assert.IsNotNull(testChild);

        var parents = await _client.Relationships.GetParentsAsync(testChild);
        Assert.IsTrue(parents.Lists.ContainsKey(_config.TestParent));
    }

    [TestMethod]
    public async Task GetChildrenAsync()
    {
        var children = await _client.Relationships.GetChildrenAsync(_config.TestParent);

        Assert.IsNotNull(children);
        Assert.IsNotNull(children.Response);
        Assert.IsNotNull(children.Response.Docs);
        Assert.IsNotNull(children.Response.NumFound);
        Assert.IsNotNull(children.Response.Start);
    }

    [TestMethod]
    public async Task GetParentsAsync()
    {
        using var parents = await _client.Relationships.GetParentsAsync(_config.TestChild);

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