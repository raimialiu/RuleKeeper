namespace RuleKeeper.Core.Analysis;

/// <summary>
/// Progress information for analysis.
/// </summary>
public class AnalysisProgress
{
    public required string Stage { get; init; }
    public int Current { get; init; }
    public int Total { get; init; }
    public string? CurrentFile { get; init; }

    public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;
}