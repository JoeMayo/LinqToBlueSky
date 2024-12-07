namespace LinqToBlueSky.Feed;

public class AccountViewer
{
    public bool Muted { get; set; }
    public bool BlockedBy { get; set; }
    public string? Following { get; set; }
    public string? FollowedBy { get; set; }
}