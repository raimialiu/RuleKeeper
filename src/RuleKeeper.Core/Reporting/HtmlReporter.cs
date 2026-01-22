using System.Text;
using System.Web;
using RuleKeeper.Core.Analysis;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Sdk;

namespace RuleKeeper.Core.Reporting;

/// <summary>
/// Generates HTML-formatted reports.
/// </summary>
public class HtmlReporter : IReportGenerator
{
    public string Format => "html";

    public string Generate(AnalysisReport report, OutputConfig config)
    {
        var summary = report.GetSummary();
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("  <title>RuleKeeper Analysis Report</title>");
        sb.AppendLine(GetStyles());
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Header
        sb.AppendLine("  <div class=\"container\">");
        sb.AppendLine("    <header>");
        sb.AppendLine("      <h1>RuleKeeper Analysis Report</h1>");
        sb.AppendLine($"      <p class=\"timestamp\">Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine("    </header>");

        // Summary section
        sb.AppendLine("    <section class=\"summary\">");
        sb.AppendLine("      <h2>Summary</h2>");
        sb.AppendLine("      <div class=\"summary-grid\">");
        sb.AppendLine($"        <div class=\"summary-item\"><span class=\"label\">Path</span><span class=\"value\">{HttpUtility.HtmlEncode(report.AnalyzedPath)}</span></div>");
        sb.AppendLine($"        <div class=\"summary-item\"><span class=\"label\">Files Analyzed</span><span class=\"value\">{summary.TotalFiles}</span></div>");
        sb.AppendLine($"        <div class=\"summary-item\"><span class=\"label\">Duration</span><span class=\"value\">{summary.Duration.TotalSeconds:F2}s</span></div>");
        sb.AppendLine($"        <div class=\"summary-item\"><span class=\"label\">Total Violations</span><span class=\"value\">{summary.TotalViolations}</span></div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </section>");

        // Severity breakdown
        sb.AppendLine("    <section class=\"severity-breakdown\">");
        sb.AppendLine("      <h2>Violations by Severity</h2>");
        sb.AppendLine("      <div class=\"severity-bars\">");
        sb.AppendLine(GenerateSeverityBar("Critical", summary.CriticalCount, summary.TotalViolations, "critical"));
        sb.AppendLine(GenerateSeverityBar("High", summary.HighCount, summary.TotalViolations, "high"));
        sb.AppendLine(GenerateSeverityBar("Medium", summary.MediumCount, summary.TotalViolations, "medium"));
        sb.AppendLine(GenerateSeverityBar("Low", summary.LowCount, summary.TotalViolations, "low"));
        sb.AppendLine(GenerateSeverityBar("Info", summary.InfoCount, summary.TotalViolations, "info"));
        sb.AppendLine("      </div>");
        sb.AppendLine("    </section>");

        // Violations by file
        if (report.Violations.Count > 0)
        {
            sb.AppendLine("    <section class=\"violations\">");
            sb.AppendLine("      <h2>Violations</h2>");

            foreach (var fileGroup in report.ViolationsByFile.OrderBy(g => g.Key))
            {
                var relativePath = GetRelativePath(fileGroup.Key, report.AnalyzedPath);
                sb.AppendLine($"      <div class=\"file-group\">");
                sb.AppendLine($"        <h3 class=\"file-path\">{HttpUtility.HtmlEncode(relativePath)} <span class=\"count\">({fileGroup.Value.Count})</span></h3>");
                sb.AppendLine("        <table class=\"violations-table\">");
                sb.AppendLine("          <thead><tr><th>Line</th><th>Severity</th><th>Rule</th><th>Message</th></tr></thead>");
                sb.AppendLine("          <tbody>");

                foreach (var violation in fileGroup.Value.OrderBy(v => v.StartLine))
                {
                    sb.AppendLine(GenerateViolationRow(violation, config));
                }

                sb.AppendLine("          </tbody>");
                sb.AppendLine("        </table>");
                sb.AppendLine("      </div>");
            }

            sb.AppendLine("    </section>");
        }

        // Errors
        if (report.Errors.Count > 0)
        {
            sb.AppendLine("    <section class=\"errors\">");
            sb.AppendLine("      <h2>Errors</h2>");
            sb.AppendLine("      <ul>");
            foreach (var error in report.Errors)
            {
                sb.AppendLine($"        <li><strong>{HttpUtility.HtmlEncode(error.FilePath)}</strong>: {HttpUtility.HtmlEncode(error.Message)}</li>");
            }
            sb.AppendLine("      </ul>");
            sb.AppendLine("    </section>");
        }

        // Footer
        sb.AppendLine("    <footer>");
        sb.AppendLine("      <p>Generated by RuleKeeper</p>");
        sb.AppendLine("    </footer>");
        sb.AppendLine("  </div>");

        sb.AppendLine(GetScripts());
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    public async Task WriteToFileAsync(AnalysisReport report, OutputConfig config, string filePath)
    {
        var content = Generate(report, config);
        await File.WriteAllTextAsync(filePath, content);
    }

    private static string GenerateSeverityBar(string label, int count, int total, string cssClass)
    {
        var percentage = total > 0 ? (double)count / total * 100 : 0;
        return $@"        <div class=""severity-row"">
          <span class=""severity-label {cssClass}"">{label}</span>
          <div class=""bar-container"">
            <div class=""bar {cssClass}"" style=""width: {percentage:F1}%""></div>
          </div>
          <span class=""count"">{count}</span>
        </div>";
    }

    private static string GenerateViolationRow(Violation v, OutputConfig config)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"            <tr class=\"severity-{v.Severity.ToString().ToLower()}\">");
        sb.AppendLine($"              <td class=\"line\">{v.StartLine}:{v.StartColumn}</td>");
        sb.AppendLine($"              <td class=\"severity\"><span class=\"badge {v.Severity.ToString().ToLower()}\">{v.Severity}</span></td>");
        sb.AppendLine($"              <td class=\"rule\">{HttpUtility.HtmlEncode(v.RuleId)}</td>");
        sb.AppendLine($"              <td class=\"message\">");
        sb.AppendLine($"                <span>{HttpUtility.HtmlEncode(v.Message)}</span>");

        if (config.ShowCode && !string.IsNullOrEmpty(v.CodeSnippet))
        {
            sb.AppendLine($"                <pre class=\"code-snippet\">{HttpUtility.HtmlEncode(v.CodeSnippet.Trim())}</pre>");
        }

        if (config.ShowHints && !string.IsNullOrEmpty(v.FixHint))
        {
            sb.AppendLine($"                <div class=\"fix-hint\">Hint: {HttpUtility.HtmlEncode(v.FixHint)}</div>");
        }

        sb.AppendLine("              </td>");
        sb.AppendLine("            </tr>");

        return sb.ToString();
    }

    private static string GetRelativePath(string fullPath, string basePath)
    {
        try
        {
            return Path.GetRelativePath(basePath, fullPath);
        }
        catch
        {
            return fullPath;
        }
    }

    private static string GetStyles()
    {
        return @"  <style>
    :root {
      --critical: #dc2626;
      --high: #ea580c;
      --medium: #ca8a04;
      --low: #0891b2;
      --info: #6b7280;
      --bg: #f9fafb;
      --card-bg: #ffffff;
      --text: #1f2937;
      --border: #e5e7eb;
    }
    * { margin: 0; padding: 0; box-sizing: border-box; }
    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: var(--bg); color: var(--text); line-height: 1.6; }
    .container { max-width: 1200px; margin: 0 auto; padding: 2rem; }
    header { margin-bottom: 2rem; }
    header h1 { font-size: 2rem; margin-bottom: 0.5rem; }
    .timestamp { color: var(--info); }
    section { background: var(--card-bg); border-radius: 8px; padding: 1.5rem; margin-bottom: 1.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.1); overflow: hidden; }
    h2 { font-size: 1.25rem; margin-bottom: 1rem; padding-bottom: 0.5rem; border-bottom: 1px solid var(--border); }

    /* Summary grid - fixed layout to prevent overlap */
    .summary-grid {
      display: grid;
      grid-template-columns: 1fr 120px 120px 140px;
      gap: 1.5rem;
      align-items: start;
    }
    @media (max-width: 768px) {
      .summary-grid { grid-template-columns: 1fr 1fr; }
    }
    @media (max-width: 480px) {
      .summary-grid { grid-template-columns: 1fr; }
    }
    .summary-item { display: flex; flex-direction: column; min-width: 0; }
    .summary-item .label { font-size: 0.875rem; color: var(--info); margin-bottom: 0.25rem; }
    .summary-item .value {
      font-size: 1.25rem;
      font-weight: 600;
      word-break: break-all;
      overflow-wrap: anywhere;
    }
    /* Path-specific styling */
    .summary-item:first-child .value {
      font-size: 0.9rem;
      font-family: monospace;
      background: var(--bg);
      padding: 0.5rem;
      border-radius: 4px;
      max-height: 4.5rem;
      overflow-y: auto;
    }

    .severity-bars { display: flex; flex-direction: column; gap: 0.5rem; }
    .severity-row { display: flex; align-items: center; gap: 1rem; }
    .severity-label { width: 80px; font-weight: 500; flex-shrink: 0; }
    .bar-container { flex: 1; height: 24px; background: var(--border); border-radius: 4px; overflow: hidden; min-width: 100px; }
    .bar { height: 100%; transition: width 0.3s; }
    .bar.critical, .severity-label.critical { background: var(--critical); color: white; padding: 0 0.5rem; border-radius: 4px; }
    .bar.high, .severity-label.high { background: var(--high); color: white; padding: 0 0.5rem; border-radius: 4px; }
    .bar.medium, .severity-label.medium { background: var(--medium); color: white; padding: 0 0.5rem; border-radius: 4px; }
    .bar.low, .severity-label.low { background: var(--low); color: white; padding: 0 0.5rem; border-radius: 4px; }
    .bar.info, .severity-label.info { background: var(--info); color: white; padding: 0 0.5rem; border-radius: 4px; }
    .severity-row .count { width: 50px; text-align: right; font-weight: 600; flex-shrink: 0; }
    .file-group { margin-bottom: 1.5rem; }
    .file-path { font-size: 1rem; font-weight: 600; color: #2563eb; margin-bottom: 0.5rem; word-break: break-all; }
    .file-path .count { font-weight: normal; color: var(--info); }
    .violations-table { width: 100%; border-collapse: collapse; font-size: 0.875rem; table-layout: fixed; }
    .violations-table th, .violations-table td { padding: 0.75rem; text-align: left; border-bottom: 1px solid var(--border); vertical-align: top; }
    .violations-table th { background: var(--bg); font-weight: 600; }
    .violations-table .line { width: 80px; font-family: monospace; }
    .violations-table .severity { width: 100px; }
    .violations-table .rule { width: 140px; font-family: monospace; word-break: break-all; }
    .violations-table .message { word-break: break-word; }
    .badge { padding: 0.25rem 0.5rem; border-radius: 4px; font-size: 0.75rem; font-weight: 600; color: white; display: inline-block; }
    .badge.critical { background: var(--critical); }
    .badge.high { background: var(--high); }
    .badge.medium { background: var(--medium); }
    .badge.low { background: var(--low); }
    .badge.info { background: var(--info); }
    .code-snippet { margin-top: 0.5rem; padding: 0.5rem; background: #1f2937; color: #e5e7eb; border-radius: 4px; font-family: monospace; font-size: 0.8rem; overflow-x: auto; white-space: pre-wrap; word-break: break-all; }
    .fix-hint { margin-top: 0.5rem; padding: 0.5rem; background: #ecfdf5; color: #065f46; border-radius: 4px; font-size: 0.8rem; }
    .errors ul { list-style: none; }
    .errors li { padding: 0.5rem; background: #fef2f2; border-radius: 4px; margin-bottom: 0.5rem; color: #991b1b; word-break: break-all; }
    footer { text-align: center; color: var(--info); font-size: 0.875rem; margin-top: 2rem; }
  </style>";
    }

    private static string GetScripts()
    {
        return @"  <script>
    // Add any interactive features here
  </script>";
    }
}
