# Changelog

### 2.0.0

- Add Wayback API
- Add Multipart Upload support
- Wrap all async calls in .ConfigureAwait(false)
- Update to System.Text.Json 7.0.0
- Breaking change to ``Item.DeleteAsync`` (use DeleteRequest.RemoteFilename)

### 1.0.0

- Initial release