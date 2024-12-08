namespace LinqToBlueSky.Feed;

public class External
{
    public string? Description { get; set; }

    /// <summary>
    /// Needed to make this dynamic because the Thumb property can be either a Thumb object or a string.
    /// </summary>
    public dynamic? Thumb { get; set; }

    public string? Title { get; set; }

    public string? Uri { get; set; }
}