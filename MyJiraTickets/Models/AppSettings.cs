using System.Text.Json;
using System.IO;

namespace MyJiraTickets.Models;

public class JiraSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ExtraApiHeaderName { get; set; } = "Authorization";
}

public class AppSettings
{
    public JiraSettings Jira { get; set; } = new JiraSettings();
    public string StartHtmlPath { get; set; } = @"C:\\_WA\\Extensions\\StartExtension\\start.html";
    public string HtmlContentId { get; set; } = string.Empty; // Empty = generate full HTML, otherwise inject into existing div
    public string Mode { get; set; } = "manual"; // "manual" or "jira"
    public string DatabasePath { get; set; } = @"D:\ticketdb\tickets.db";

    public static AppSettings Load(string path)
    {
        string? resolved = null;
        // 1. Direct path
        if (File.Exists(path)) resolved = path;
        // 2. Base directory of application
        if (resolved == null)
        {
            var baseDir = AppContext.BaseDirectory;
            var candidate = Path.Combine(baseDir, Path.GetFileName(path));
            if (File.Exists(candidate)) resolved = candidate;
        }
        // 3. Current directory
        if (resolved == null)
        {
            var candidate = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(path));
            if (File.Exists(candidate)) resolved = candidate;
        }
        if (resolved == null)
            return new AppSettings();
        var json = File.ReadAllText(resolved);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    public void Save(string path)
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
