# Changelog

### 6.0.0

- Now requires .NET 8 or later
- Update dependencies

### 5.0.0

- **Breaking change**: Internet Archive removed Wayback API functionality coinciding with their Google partnership. 
``WayBack.IsAvailableAsync``, ``WayBack.GetNumPagesAsync``, and the ``Wayback.SearchRequest.Offset`` parameter are no longer available.
- Update to latest ``System.Text.Json``

### 4.2.0

- Add ``SetTimeout`` method to ``Client``
- Update to C# 12
- Update ``System.Text.Json`` and other dependencies to 8.0.x

### 4.1.0

- Add ``ProgressChanged`` event on ``Item.PutRequest`` to track upload progress
- Optimization to split Multipart Uploads evenly across threads
- Remove dependency on deprecated ``Microsoft.AspNetCore.WebUtilities``

### 4.0.0

- **Breaking change**: Metadata is now returned as ``JsonElement`` instead of ``JsonDocument``.
This removes the need to worry about ``using`` statements and lifecycle issues when calling the library.

```csharp
 // old
  using var response = await archive.Metadata.ReadAsync("morphtoyharing");
  Console.WriteLine(response.Metadata?.RootElement.GetProperty("description"));

 // new
  var response = await archive.Metadata.ReadAsync("morphtoyharing");
  Console.WriteLine(response.Metadata.Value.GetProperty("description"));
```

### 3.0.0

- **Breaking change**: ``Wayback.IsAvailable`` -> ``Wayback.IsAvailableAsync``
- **Breaking change**: throw `InternetArchiveRequestException` instead of ``HttpRequestException``
- Add Wayback CDX API
- Add optional `CancellationToken` parameter to all methods
- Add `SourceStream` option to `Item.PutRequest`
- Add `InternetArchiveException`
- Add `InternetArchiveRequestException`
- Add `InternetArchiveResponseException`

### 2.0.0

- **Breaking change** to ``Item.DeleteAsync`` (fill in ``DeleteRequest.RemoteFilename``)
- Add Wayback API
- Add Multipart Upload support
- Wrap all async calls in .ConfigureAwait(false)
- Update to ``System.Text.Json`` 7.0.0

### 1.0.0

- Initial release
