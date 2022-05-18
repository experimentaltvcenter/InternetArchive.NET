namespace InternetArchive;

public class ServerResponseException : Exception
{
    public ServerResponseException(string message, Exception? ex = null) : base(message, ex) { }
}

public class ServerResponse
{
    public bool? Success { get; set; }

    public void EnsureSuccess()
    {
        if (Success == null) throw new ServerResponseException("server returned success == null");
        if (Success == false) throw new ServerResponseException("server returned success == false");
    }
}
