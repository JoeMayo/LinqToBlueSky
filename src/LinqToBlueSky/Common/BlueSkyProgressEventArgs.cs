namespace LinqToBlueSky.Common;

/// <summary>
/// Lets caller know the percentage of completion of operation
/// </summary>
public class BlueSkyProgressEventArgs : EventArgs
{
    /// <summary>
    /// Percentage of completion
    /// </summary>
    public int PercentComplete { get; set; }
}
