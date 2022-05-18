 # Troubleshooting

The Internet Archive is an amazing resource, but also subject to abuse and attacks from bad actors.

* HTTP 429 ("TooManyRequests") errors that do not resolve may be an indication of a flagged account or IP address.

* Downtime may be more frequent than you've come to expect from highly componentized services provided by trillion dollar corporate cloud vendors. Design and code defensively and expect [three nines](https://en.wikipedia.org/wiki/High_availability#Percentage_calculation).

* HTTP 400 and HTTP 500 errors may appear during periods of high load. Check logs and note the informal text sometimes included in HTTP error responses.

* Note the HttpClient setup in [ServiceExtensions.cs](../InternetArchive.NET/ServiceExtensions.cs) including cookies, redirects, and 100-Continue handling.

* Also note the [Microsoft.Extensions.Http.Polly](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly) retry logic in [ServiceExtensions.cs](../InternetArchive.NET/ServiceExtensions.cs). If you discover additional failure modes please [submit an issue](https://github.com/experimentaltvcenter/InternetArchive.NET/issues).

* The default HTTP timeout is 15 minutes which also sets the maximum file upload time. You can [override it](https://github.com/experimentaltvcenter/InternetArchive.NET/blob/main/InternetArchive.NET/ServiceExtensions.cs#L13).

## Support

* For help with this library please [submit an issue](https://github.com/experimentaltvcenter/InternetArchive.NET/issues).

* Check the [top of archive.org](https://archive.org) or [@internetarchive](https://twitter.com/internetarchive) on Twitter for archive.org service status.

* For assistance with archive.org account issues, email [info@archive.org](mailto:info@archive.org).