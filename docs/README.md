# Documentation

* [Setup](#setup)
* [Create an API Client](#create-an-api-client)
* [Client Configuraiton](#client-configuration)
* [Changes API](#changes-api)
* [Item API](#item-api)
* [Metadata API](#metadata-api)
* [Relationships API](#relationships-api)
* [Reviews API](#reviews-api)
* [Search API](#search-api)
* [Tasks API](#tasks-api)
* [Views API](#views-api)

For an overview of archive.org services, view the archive.org documentation at https://archive.org/services/docs/api/index.html.

<br />

# Setup

Install the [InternetArchive.NET](https://www.nuget.org/packages/InternetArchive.NET/) package located on NuGet.

The library is Dependency Injection aware but requires a minimal amount of ceremony to get started. By default, the library registers a custom HttpClient configured with retry policies tuned to archive.org.

You may override the defaults by calling ``ServiceExtensions.AddInternetArchiveServices()`` and/or ``ServiceExtensions.AddInternetArchiveDefaultRetryPolicies()`` before creating a client. 

Note that ``AddInternetArchiveServices()`` sets a default HTTP timeout of 15 minutes which sets the maximum upload time for a file.

<br />

# Create an API Client

Create a read-only client:

```csharp
 var archive = Client.CreateReadOnly();
```
Enable write access using API keys (see https://archive.org/account/s3.php).

```csharp
 var archive = Client.Create(string accessKey, string secretKey);
```

Slowest (requiring an extra API call to retrieve keys). If a value isn't passed in, the user is prompted via console, making this useful for interactive scenarios.

```csharp
var archive = await Client.CreateAsync(string emailAddress, string password);
```

<br />

# Client Configuration

Request a priority boost. Note restrictions and please use responsibly. https://archive.org/services/docs/api/ias3.html#express-queue

```csharp
archive.RequestInteractivePriority();
```

Perform a dry-run. In this mode, functions that modify data do not contact the server and return NULL.

```csharp
archive.DryRun = true;
```

<br />

# Changes API
Retrieve a list of changes made to items at the archive.<br />https://archive.org/services/docs/api/changes.html

First, choose a starting point:

```csharp
var response = await archive.Changes.GetFromBeginningAsync();
```

```csharp
var response = await archive.Changes.GetStartingNowAsync();
```

```csharp
var response = await archive.Changes.GetAsync(DateOnly startDate);
```

```csharp
var response = await archive.Changes.GetAsync(DateTime startDate);
```

Then page through additional results using the response token:

```csharp
var response = await archive.Changes.GetAsync(response.Token);
```

<br />

# Item API
Create and delete items and files at the archive.<br />https://archive.org/services/docs/api/ias3.html


Check quotas and global limits:

```csharp
var response = await archive.Item.GetUseLimitAsync();
```

Create an item (similar to an Amazon S3 bucket):

```csharp
var metadata = new List<KeyValuePair<string, object?>>
{
    new KeyValuePair<string, object?>("collection", "test_collection"),
    new KeyValuePair<string, object?>("mediatype", "texts")
};

await archive.Item.PutAsync(new Item.PutRequest
{
    Bucket = "my-identifier",
    LocalPath = "/path/to/local/file.txt",
    RemoteFilename = "hello.txt",
    Metadata = metadata,
    CreateBucket = true
});
```

Add a file to a bucket:

```csharp
await archive.Item.PutAsync(new Item.PutRequest
{
    Bucket = "my-identifier",
    LocalPath = "/path/to/local/file.txt",
    RemoteFilename = "hello-again.txt",
});
```

Delete a file from a bucket:

```csharp
await archive.Item.DeleteAsync(new Item.DeleteRequest
{
    Bucket = $"my_identifier/hello-again.txt",
    CascadeDelete = true,
    KeepOldVersion = false
});
```

Deleting an item in its entirety requires admin privileges. You can "dark" (hide) an item using the [Tasks API](#tasks-api).

<br />

# Metadata API
Read or write JSON metadata for an archive item.<br />https://archive.org/services/docs/api/metadata.html


```csharp
using var response = await archive.Metadata.ReadAsync("morphtoyharing"); // IDisposable
Console.WriteLine(response.Metadata?.RootElement.GetProperty("description"));
```

```csharp
var patch = new JsonPatchDocument();
patch.Add("/year", "1900");
await archive.Metadata.WriteAsync("TheMagicianSilentFilm1900", patch);
```

<br />

# Relationships API
Establish and explore relationships between items.<br />https://archive.org/services/docs/api/simplelists.html

Query relationships:

```csharp
string parent = "library_of_atlantis";
string child = "nowyourespeaking00chap";

using var parents = await archive.Relationships.GetParentsAsync(child); // IDisposable
var children = await archive.Relationships.GetChildrenAsync(parent);
```

Create relationships:

```csharp
string parent = "collection_name"; // must be a collection, must have write access
string list = "my_list"; // must be unique
string identifier = "nowyourespeaking00chap";

await archive.Relationships.AddAsync(identifier, parent, list);
await archive.Relationships.RemoveAsync(identifier, parent, list);
```

<br />

# Reviews API
Add reviews to archive.org items<br />https://archive.org/services/docs/api/reviews.html

Add or update a review:

```csharp
var request = new Reviews.AddOrUpdateRequest
{
    Title = "Title Text",
    Body = "Body text",
    Identifier = "nowyourespeaking00chap",
    Stars = 3
};

var response = await archive.Reviews.AddOrUpdateAsync(request);
```

Delete your review:

```csharp
var response = await archive.Reviews.DeleteAsync("nowyourespeaking00chap");
```

Read your review. To retrieve all reviews, use the [Metadata API](#metadata-api) instead.

```csharp
var response = await archive.Reviews.GetAsync("nowyourespeaking00chap");
```

<br />

# Search API

Search the archive.<br />https://archive.org/help/aboutsearch.htm

```csharp
var request = new Search.ScrapeRequest
{
    Query = "scanimate",
    Fields = new[] { "identifier", "title", "description" }, // fields to retrieve
    Sorts = new[] { "title" }
};

var response = await archive.Search.ScrapeAsync(request);

// note: ScrapeAsync attempts to map archive.org schema to a typed class.
// In case of problems, file an issue and use this as a workaround:

using var jsonDocument = await archive.Search.ScrapeAsJsonAsync(request); // IDisposable
```

To explore search parameters, visit https://archive.org/advancedsearch.php.

<br />

# Tasks API
Submit tasks to update items at the archive.<br />https://archive.org/services/docs/api/tasks.html

Submit a task:

```csharp
var response = await archive.Tasks.SubmitAsync(identifier, Tasks.Command.MakeDark);
```

Re-run a task:
```csharp
var response = await archive.RerunAsync(taskId);
```

Get logs for a task:
```csharp
var response = await archive.GetLogAsync(taskId);
```

Get task rate limits for a command:
```csharp
var response = await archive.GetRateLimitAsync(Tasks.Command.Derive);
```

Get pending tasks for an item:
```csharp
var request = new Tasks.GetRequest 
{ 
    Identifier = "morphtoyharing", 
    Catalog = true, 
    History = true
};

var response = await archive.Tasks.GetAsync(request);
```

Get pending tasks for an account:
```csharp
var request = new Tasks.GetRequest { Submitter = "email@example.com" };
var response = await archive.Tasks.GetAsync(request);
```

<br />

# Views API
Retrieve aggregated view data for items and collections.<br />
https://archive.org/services/docs/api/views_api.html


```csharp
GetItemSummaryAsync(string identifier, bool legacy = false)
GetItemSummaryAsync(IEnumerable<string> identifiers, bool legacy = false)

// <T> is DateTime or DateOnly

GetItemSummaryPerDayAsync<T>(string identifier)
GetItemSummaryPerDayAsync<T>(IEnumerable<string> identifiers)

GetItemDetailsAsync<T>(string identifier, T startDate, T endDate)
GetCollectionDetailsAsync<T>(string collection, T startDate, T endDate)
```