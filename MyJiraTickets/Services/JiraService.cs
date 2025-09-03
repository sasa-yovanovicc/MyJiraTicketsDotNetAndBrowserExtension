using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MyJiraTickets.Services;


public class JiraService
{
    private readonly string _baseUrl;
    private readonly string _username;
    private readonly string _password;
    private readonly string _apiKey;

    public JiraService(string baseUrl, string username, string password, string apiKey)
    {
        _baseUrl = baseUrl?.Trim().TrimEnd('/') ?? string.Empty;
        _username = username ?? string.Empty;
        _password = password ?? string.Empty;
        _apiKey = apiKey ?? string.Empty;
    }

    public record JiraTicket(string Key, string Url, string Summary, string Status, string Type, string Priority);

    public async Task<List<JiraTicket>> GetAllTicketsAsync(string jql = "project = SCRUM ORDER BY updated DESC", int maxResults = 50)
    {
        var tickets = new List<JiraTicket>();
        if (string.IsNullOrWhiteSpace(_baseUrl)) 
        {
            return tickets;
        }
        if (string.IsNullOrWhiteSpace(_username)) 
        {
            return tickets;
        }
        if (string.IsNullOrWhiteSpace(_password)) 
        {
            return tickets;
        }

        using var client = new HttpClient();
        
        // Use API token as password in Basic auth (recommended approach)
        var authString = !string.IsNullOrEmpty(_apiKey) ? $"{_username}:{_apiKey}" : $"{_username}:{_password}";
        var authToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(authString));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        Console.WriteLine($"Using auth: {_username}:{(_apiKey?.Substring(0, 10) ?? _password?.Substring(0, 3))}...");

        var apiPaths = new[] { "/rest/api/2/search", "/rest/api/3/search" };
        foreach (var api in apiPaths)
        {
            var url = $"{_baseUrl.TrimEnd('/')}{api}?jql={Uri.EscapeDataString(jql)}&maxResults={maxResults}";
            try
            {
                var response = await client.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    if (api.EndsWith("/api/3/search"))
                        return tickets;
                    continue;
                }
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("issues", out var issues))
                {
                    foreach (var issue in issues.EnumerateArray())
                    {
                        var key = issue.GetProperty("key").GetString() ?? "";
                        var urlIssue = $"{_baseUrl.TrimEnd('/')}/browse/{key}";
                        var fields = issue.GetProperty("fields");
                        var summary = fields.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "";
                        var status = fields.TryGetProperty("status", out var st) && st.TryGetProperty("name", out var stn) ? stn.GetString() ?? "" : "";
                        var type = fields.TryGetProperty("issuetype", out var it) && it.TryGetProperty("name", out var itn) ? itn.GetString() ?? "Task" : "Task";
                        var priority = fields.TryGetProperty("priority", out var pr) && pr.TryGetProperty("name", out var prn) ? prn.GetString() ?? "Medium" : "Medium";
                        tickets.Add(new JiraTicket(key, urlIssue, summary, status, type, priority));
                    }
                }
                return tickets;
            }
            catch { return tickets; }
        }
        return tickets;
    }

    public record JiraIssueResult(bool Success, string? Summary, string? Status, string? Type, string? Priority, string? ErrorMessage, int StatusCode = 0, string? Raw = null);

    public async Task<JiraIssueResult> GetIssueSummaryDetailedAsync(string issueKey)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl)) return new(false, null, null, null, null, "BaseUrl nije podešen");
        if (string.IsNullOrWhiteSpace(_username)) return new(false, null, null, null, null, "Username nije podešen");
        if (string.IsNullOrWhiteSpace(_apiKey)) return new(false, null, null, null, null, "ApiKey nije podešen (potreban za Basic auth)");

        using var client = new HttpClient();
        // Basic auth: email:token (kao u testovima koji rade)
        var authToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_username}:{_apiKey}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Probaj v2 pa v3 (neke instance koriste v3)
        var paths = new[] { $"{_baseUrl}/rest/api/2/issue/{issueKey}", $"{_baseUrl}/rest/api/3/issue/{issueKey}" };
        foreach (var url in paths)
        {
            try
            {
                var response = await client.GetAsync(url);
                var status = (int)response.StatusCode;
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    // 404 ili auth problem -> probaj sledeći ako ima
                    if (url.EndsWith("/api/3/issue/" + issueKey) || status == 401 || status == 403)
                        return new(false, null, null, null, null, $"HTTP {status}: {response.ReasonPhrase}", status, body);
                    continue;
                }
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("fields", out var fields))
                    {
                        var summary = fields.TryGetProperty("summary", out var summaryProp) ? summaryProp.GetString() : "";
                        var ticketStatus = fields.TryGetProperty("status", out var st) && st.TryGetProperty("name", out var stn) ? stn.GetString() : "";
                        var type = fields.TryGetProperty("issuetype", out var it) && it.TryGetProperty("name", out var itn) ? itn.GetString() : "Task";
                        var priority = fields.TryGetProperty("priority", out var pr) && pr.TryGetProperty("name", out var prn) ? prn.GetString() : "Medium";
                        
                        return new(true, summary, ticketStatus, type, priority, null, status);
                    }
                    return new(false, null, null, null, null, "Nedostaje fields u JSON", status, body);
                }
                catch (Exception ex)
                {
                    return new(false, null, null, null, null, "Parse error: " + ex.Message, status, body);
                }
            }
            catch (Exception ex)
            {
                return new(false, null, null, null, null, ex.Message);
            }
        }
        return new(false, null, null, null, null, "Neuspešno dohvaćanje (v2 i v3)");
    }

    // Stari interfejs za kompatibilnost
    public async Task<string?> GetIssueSummaryAsync(string issueKey)
    {
        var res = await GetIssueSummaryDetailedAsync(issueKey);
        return res.Success ? res.Summary : null;
    }
}
