namespace InternetArchiveTests;

internal class Config
{
    public string LocalFilename { get; } = "test.txt";
    public string RemoteFilename { get; } = "hello.txt";

    public string TestList { get; set; } = string.Empty;
    public string TestCollection { get; set; } = string.Empty;

    public string TestParent { get; set; } = string.Empty;
    public string TestChild { get; set; } = string.Empty;

    public string EmailAddress { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    public bool CanDelete { get; set; }
}
