# Contributor Documentation

## Design Philosophy

IntenetArchive.NET is a type safe wrapper around an [evolving schema](https://archive.org/services/docs/api/metadata-schema/index.html) developed using PHP on the server and consumed by a [Python client library](https://github.com/jjjake/internetarchive). API evolved over nearly two decades using a variety of technical conventions. Many API remain in beta.

As such, the goal is to *gently harmonize* and make things feel familiar to .NET programmers.

API and field names were generally left as-is. Nullable properties are used by default to provide an additional degree of safety.

## Data Harmonization

By way of example, archive.org uses no fewer than seven different date formats. We convert to ``DateTimeOffset`` when UTC. In the absence of time information, we use ``DateOnly`` with fallback to ``DateTime`` on .NET Standard. Sometimes non-date information is stored in date fields forcing use of ``string``.

Visit [JsonConvertors.cs](../InternetArchive.NET/JsonConverters.cs) to see the state of the battle.

Integer fields like "width" sometimes, but not always, return strings. These are notated when discovered using ``[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]``.

Sorting out the schema across nearly 700 billion items is important work and we appreciate you letting us know any issues you discover.

## System.Text.Json

Users of the Internet Archive can attach arbitrary JSON to items. Instead of dynamics and ``ExpandoObject`` we decided to standardize on [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/api/system.text.json).

## Unit Tests

To run tests, first [create an account on archive.org](https://archive.org/account/signup). 

Then copy [appsettings.json](../InternetArchive.NET.Tests/appsettings.json) to ``appsettings.private.json`` and fill in the missing fields:

* Set ``emailAddress`` to your archive.org account email address
* Set ``accessKey`` and ``secretKey`` to your [archive.org keys](https://archive.org/account/s3.php) (or set environment variables)
* Optionally set ``canDelete`` to ``true`` if you have admin rights. If not, items will be darked instead of deleted.
* Optionally fill in ``testList`` and ``testCollection`` if you have them. If not, part of RelationshipTests will be ignored. (This is ok.)

Do not share or post your appsetings.private.json file. (It is already included in .gitignore.)

## Continuous Integration

Running unit tests against a public resource is tricky. New accounts may be flagged for spam. An account can only add ten reviews a day. Without admin access, you cannot delete items and clean up after yourself. Testing functionality like "SimpleLists" requires objects that only archive.org admins can create for you.

So &ndash; create tests, and definitely run them. But trust your instincts if they fail suspiciously. Sometimes "try again tomorrow" is the correct approach.

If unrelated tests fail when you submit a pull request, don't worry. We'll verify everything before merging changes.

See [Troubleshooting](./TROUBLESHOOTING.md) for more details.