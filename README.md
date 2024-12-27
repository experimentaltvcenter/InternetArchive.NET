# InternetArchive.NET

[![Build Status](https://github.com/experimentaltvcenter/InternetArchive.NET/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/experimentaltvcenter/InternetArchive.NET/actions/workflows/build-and-test.yml)
[![NuGet version (InternetArchive.NET)](https://img.shields.io/nuget/v/InternetArchive.NET.svg?style=flat-square)](https://www.nuget.org/packages/InternetArchive.NET/)

.NET library providing access to API services at [Internet Archive](https://archive.org) (archive.org).

- MIT License
- Targets .NET 8 on Linux, Mac and Windows and .NET Standard 2.0
- Uses Microsoft.Extensions.Http.Polly for [retry logic](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly)
- Only three dependencies (all from Microsoft)

## Quick Start

```csharp
using InternetArchive;

var archive = Client.CreateReadOnly();
var response = await archive.Search.ScrapeAsync(new Search.ScrapeRequest
{
    Query = "TRS-80",
    Fields = [ "identifier", "title", "description" ],
    Sorts = [ "title" ]
});
```

## Documentation

- [For library users](./docs/README.md)
- [For library contributors](./docs/DEVELOPERS.md)
- [Troubleshooting](./docs/TROUBLESHOOTING.md)
- [Changelog](./docs/CHANGELOG.md)

## About Us

[Experimental Television Center Ltd.](https://www.experimentaltvcenter.org) is a 501(c)(3) non-profit established 1971. Please support our mission to empower artists and open media with a tax-deductible [contribution](https://www.paypal.com/donate/?hosted_button_id=GM2VN43D6RSXJ).
