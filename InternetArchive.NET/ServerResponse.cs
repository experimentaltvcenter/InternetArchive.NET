namespace InternetArchive;

public class ServerResponse
{
    public bool? Success { get; set; }

    public void EnsureSuccess()
    {
        if (Success == null) throw new InternetArchiveResponseException("server returned success == null");
        if (Success == false) throw new InternetArchiveResponseException("server returned success == false");
    }
}
