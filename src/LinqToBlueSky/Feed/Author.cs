﻿namespace LinqToBlueSky.Feed;

public class Author
{
    public string? Did { get; set; }
    public string? Handle { get; set; }
    public string? DisplayName { get; set; }
    public string? Avatar { get; set; }
    public AccountViewer? Viewer { get; set; }
    public List<Label>? Labels { get; set; }
    public DateTime CreatedAt { get; set; }
    public Associated? Associated { get; set; }
}
