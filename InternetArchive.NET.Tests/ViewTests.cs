namespace InternetArchiveTests;

[TestClass]
public class ViewTests
{
    private static readonly string _item = "adventuresoftoms00twaiiala";
    private static readonly string[] _items = [_item, "texts"];
    private static readonly string _collection = "computer-image-corporation-archive";

    private static void ValidateSummary(Views.Summary? view)
    {
        Assert.IsNotNull(view);
        Assert.IsTrue(view.HasData);

        Assert.IsNotNull(view.Last7Days);
        Assert.IsNotNull(view.Last30Days);
        Assert.IsNotNull(view.AllTime);
    }

    [TestMethod]
    public async Task GetItemSummaryAsync()
    {
        var view = await _client.Views.GetItemSummaryAsync(_item);
        ValidateSummary(view);
        Assert.IsNull(view.Detail);

        view = await _client.Views.GetItemSummaryAsync(_item, legacy: true);
        ValidateSummary(view);
        Assert.IsNull(view.Detail);

        var views = await _client.Views.GetItemSummaryAsync(_items);
        Assert.AreEqual(2, views.Count);
        ValidateSummary(views.First().Value);
    }

    private static void ValidatePerDay<T>(Views.SummaryPerDay<T>? details)
    {
        Assert.IsNotNull(details);
        Assert.IsNotNull(details.Days);

        var summary = details.Ids.First().Value;
        ValidateSummary(summary);
        ValidateDetail(summary.Detail);

        static void ValidateDetail(Views.SummaryDetail? detail)
        {
            Assert.IsNotNull(detail);
            Assert.IsNotNull(detail.Pre2017Total);

            ValidateStats(detail.Robot);
            ValidateStats(detail.NonRobot);
            ValidateStats(detail.Unrecognized);
            ValidateStats(detail.Pre2017);

            static void ValidateStats(Views.SummaryDetailStats? stats)
            {
                Assert.IsNotNull(stats);
                Assert.IsNotNull(stats.PerDay);
                Assert.IsNotNull(stats.SumPerDay);
                Assert.IsNotNull(stats.PreviousDaysTotal);
            }
        } 
    }

    [TestMethod]
    public async Task GetItemSummaryPerDayAsync()
    {
        var perDayDateTime = await _client.Views.GetItemSummaryPerDayAsync<DateTime>(_item);
        ValidatePerDay(perDayDateTime);
        Assert.AreEqual(1, perDayDateTime.Ids.Count);

        var perDayString = await _client.Views.GetItemSummaryPerDayAsync<string>(_item);
        ValidatePerDay(perDayString);
        Assert.AreEqual(1, perDayString.Ids.Count);

        var perDayDateOnly = await _client.Views.GetItemSummaryPerDayAsync<DateOnly>(_items);
        ValidatePerDay(perDayDateOnly);
        Assert.AreEqual(2, perDayDateOnly.Ids.Count);
    }

    private static void ValidateDetails<T>(Views.Details<T>? details)
    {
        Assert.IsNotNull(details);
        Assert.IsNotNull(details.Days);
        Assert.IsNotNull(details.Counts);

        var count = details.Counts.FirstOrDefault();
        Assert.IsNotNull(count);
        Assert.IsNotNull(count.Count);
        Assert.IsNotNull(count.CountKind);
        Assert.IsNotNull(count.Country);
        Assert.IsNotNull(count.GeoCountry);
        Assert.IsNotNull(count.GeoState);
        Assert.IsNotNull(count.Kind);
        Assert.IsNotNull(count.Latitude);
        Assert.IsNotNull(count.Longitude);
        Assert.IsNotNull(count.State);

        Assert.IsNotNull(details.Referers);
        if (details.Referers.Any())
        { 
            var referer = details.Referers.First();
            Assert.IsNotNull(referer.Kind);
            Assert.IsNotNull(referer.Referer);
            Assert.IsNotNull(referer.Score);
        }
    }

    [TestMethod]
    public async Task GetItemDetailsAsync()
    {
        var details = await _client.Views.GetItemDetailsAsync(_item, _startDateTime, _endDateTime);
        ValidateDetails(details);

        var details2 = await _client.Views.GetItemDetailsAsync(_item, _startDateOnly, _endDateOnly);
        ValidateDetails(details2);
    }

    [TestMethod]
    public async Task GetCollectionDetailsAsync()
    {
        var details = await _client.Views.GetCollectionDetailsAsync(_collection, _startDateTime, _endDateTime);
        ValidateDetails(details);

        var details2 = await _client.Views.GetCollectionDetailsAsync(_collection, _startDateOnly, _endDateOnly);
        ValidateDetails(details2);
    }

#if LATER // documented but not currently implemented at archive.org
    [TestMethod]
    public async Task GetContributorDetailsAsync()
    {
        var details = await _client.Views.GetContributorDetailsAsync(_contributor, _startDateTime, _endDateTime);
        ValidateDetails(details);

        var details2 = await _client.Views.GetContributorDetailsAsync(_contributor, _startDateOnly, _endDateOnly);
        ValidateDetails(details2);
    }
#endif
}