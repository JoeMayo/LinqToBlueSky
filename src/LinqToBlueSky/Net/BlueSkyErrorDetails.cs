namespace LinqToBlueSky.Net;

// TODO: Error handling is likely much different from Twitter.

public class BlueSkyErrorDetails
{
    public List<Error>? Errors { get; set; }
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public string? Type { get; set; }
    public int Status { get; set; }
}


public class Error
{
    public Dictionary<string, string[]>? Parameters { get; set; }
    public string? Message { get; set; }
    public string? Request { get; set; }
    public int Code { get; set; }
}
