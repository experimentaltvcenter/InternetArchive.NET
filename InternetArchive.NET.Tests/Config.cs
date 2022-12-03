namespace InternetArchiveTests;

internal class Config
{
    public string LocalFilename { get; } = "test.txt";
    public string RemoteFilename { get; } = "hello.txt";

    public string TestItem { get; set; } = "";  
    public string TestList { get; set; } = "";
    public string TestCollection { get; set; } = "";

    public string TestParent { get; set; } = "";
    public string TestChild { get; set; } = "";

    public string EmailAddress { get; set; } = "";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";

    public bool CanDelete { get; set; }
}
