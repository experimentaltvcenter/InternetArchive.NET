# Changelog

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

- **Breaking change** to ``Item.DeleteAsync`` (use DeleteRequest.RemoteFilename)
- Add Wayback API
- Add Multipart Upload support
- Wrap all async calls in .ConfigureAwait(false)
- Update to System.Text.Json 7.0.0

### 1.0.0

- Initial release