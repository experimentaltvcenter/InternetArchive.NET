namespace InternetArchive;

public class InternetArchiveException(string message, Exception? ex = null) : Exception(message, ex) { }

public class InternetArchiveResponseException(string message, Exception? ex = null) : InternetArchiveException(message, ex) { }

public class InternetArchiveRequestException : InternetArchiveException
{
    public InternetArchiveRequestException(HttpResponseMessage response) : base($"HTTP Error {(int)response.StatusCode}: {response.StatusCode}")
    {
        try
        {
            Body = response.Content.ReadAsStringAsync().Result;
        }
        finally
        {
            HttpResponseMessage = response;
            StatusCode = response.StatusCode;
        }
    }

    public HttpResponseMessage HttpResponseMessage { get; }
    public HttpStatusCode StatusCode { get; }
    public string? Body { get; }

    public override string ToString() { return Body == null ? Message : $"{Message} - {Body}"; }
}
