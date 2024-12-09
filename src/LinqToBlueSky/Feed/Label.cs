namespace LinqToBlueSky.Feed;

public class Label
{
    public int Ver { get; set; }
    public string? Src { get; set; }
    public string? Uri { get; set; }
    public string? Cid { get; set; }
    public string? Val { get; set; }
    public bool Neg { get; set; }
    public DateTime Exp { get; set; }
    public DateTime Cts { get; set; }
    public byte[] Sig { get; set; }
}