using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MyJiraTickets.Models;
using MyJiraTickets.Services;

namespace MyJiraTickets
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<Ticket> _tickets;
        private JiraService? _jiraService;
        private AppSettings? _appSettings;

        public MainWindow()
        {
            Console.WriteLine("=== MainWindow constructor started ===");
            InitializeComponent();
            
            _tickets = new ObservableCollection<Ticket>();
            TicketsList.ItemsSource = _tickets;

            LoadSettings();
            
            Console.WriteLine($"Settings loaded. Mode: {_appSettings?.Mode}");
            
            // Initialize Jira service if settings are available
            if (!string.IsNullOrEmpty(_appSettings?.Jira?.BaseUrl) && 
                !string.IsNullOrEmpty(_appSettings?.Jira?.Username) && 
                !string.IsNullOrEmpty(_appSettings?.Jira?.ApiKey))
            {
                _jiraService = new JiraService(
                    _appSettings.Jira.BaseUrl,
                    _appSettings.Jira.Username,
                    _appSettings.Jira.Password,
                    _appSettings.Jira.ApiKey
                );
                Console.WriteLine("Jira service initialized");
            }
            else
            {
                Console.WriteLine("Jira service not initialized - missing settings");
            }

            // Set mode based on settings but don't auto-load from Jira
            if (_appSettings?.Mode == "manual")
            {
                ManualModeBox.IsChecked = true;
                LoadTickets(); // Only load from database
            }
            else
            {
                ManualModeBox.IsChecked = false;
                LoadTickets(); // Load from database only, no auto Jira loading
                UpdateStatusMessage("Application started - click 'Load All Jira Tickets' to fetch from Jira");
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            if (settingsWindow.ShowDialog() == true)
            {
                LoadSettings();
                
                // Re-initialize Jira service if settings changed
                if (!string.IsNullOrEmpty(_appSettings?.Jira?.BaseUrl) && 
                    !string.IsNullOrEmpty(_appSettings?.Jira?.Username) && 
                    !string.IsNullOrEmpty(_appSettings?.Jira?.ApiKey))
                {
                    _jiraService = new JiraService(
                        _appSettings.Jira.BaseUrl,
                        _appSettings.Jira.Username,
                        _appSettings.Jira.Password,
                        _appSettings.Jira.ApiKey
                    );
                }
            }
        }

        private void JiraUrlBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ProcessJiraUrl();
        }

        private void JiraUrlBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ProcessJiraUrl();
            }
        }

        private void ManualModeBox_Changed(object sender, RoutedEventArgs e)
        {
            if (ManualModeBox.IsChecked == true)
            {
                // Switch to manual mode
                _appSettings.Mode = "manual";
                SaveSettings();
                LoadTickets(); // Load only from database
                UpdateStatusMessage("Switched to Manual mode - loading from database only");
            }
            else
            {
                // Switch to Jira mode but don't auto-load
                _appSettings.Mode = "jira";
                SaveSettings();
                LoadTickets(); // Load from database only
                UpdateStatusMessage("Switched to Jira mode - click 'Load All Jira Tickets' to fetch from Jira");
            }
        }

        private void StatusBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddTicketBtn.IsEnabled = !string.IsNullOrWhiteSpace(TicketNameBox.Text) && StatusBox.SelectedItem != null;
        }

        private void TypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Type selection changed
        }

        private void PriorityBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Priority selection changed
        }

        private void AddTicket_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TicketNameBox.Text) || StatusBox.SelectedItem == null)
                return;

            var jiraUrl = JiraUrlBox.Text.Trim();
            var ticketName = TicketNameBox.Text.Trim();
            var status = ((ComboBoxItem)StatusBox.SelectedItem).Content.ToString();
            var type = TypeBox.SelectedItem != null ? ((ComboBoxItem)TypeBox.SelectedItem).Content.ToString() : "Task";
            var priority = PriorityBox.SelectedItem != null ? ((ComboBoxItem)PriorityBox.SelectedItem).Content.ToString() : "Medium";

            // Extract ticket key from URL or use provided name
            string ticketKey = ticketName;
            if (!string.IsNullOrEmpty(jiraUrl))
            {
                var match = Regex.Match(jiraUrl, @"([A-Z]+-\d+)");
                if (match.Success)
                {
                    ticketKey = match.Groups[1].Value;
                }
            }

            var ticket = new Ticket
            {
                Key = ticketKey,
                Summary = ticketName,
                Status = status,
                Url = jiraUrl,
                Type = type,
                Priority = priority
            };

            try
            {
                TicketDatabase.AddTicket(ticket);
                _tickets.Add(ticket);
                
                // Clear inputs
                JiraUrlBox.Text = "";
                TicketNameBox.Text = "";
                StatusBox.SelectedItem = null;
                TypeBox.SelectedItem = null;
                PriorityBox.SelectedItem = null;
                AddTicketBtn.IsEnabled = false;
                
                UpdateStatusMessage($"Added ticket: {ticketKey}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding ticket: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteSelectedTicket_Click(object sender, RoutedEventArgs e)
        {
            if (TicketsList.SelectedItem is not Ticket selectedTicket)
                return;

            // Show custom confirmation dialog
            var confirmDialog = new DeleteConfirmationWindow(selectedTicket)
            {
                Owner = this
            };

            bool? result = confirmDialog.ShowDialog();

            if (result == true && confirmDialog.Confirmed)
            {
                try
                {
                    TicketDatabase.DeleteTicket(selectedTicket.Key);
                    _tickets.Remove(selectedTicket);
                    UpdateStatusMessage($"Deleted ticket: {selectedTicket.Key}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting ticket: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ticketList = _tickets.ToList();
                TicketDatabase.SaveTickets(ticketList);
                UpdateStatusMessage("Changes saved successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFromHtml_Click(object sender, RoutedEventArgs e)
        {
            var startHtmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StartExtension", "start.html");
            
            if (!File.Exists(startHtmlPath))
            {
                MessageBox.Show("start.html file not found in StartExtension folder", "File Not Found", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var htmlContent = File.ReadAllText(startHtmlPath);
                var tickets = ParseTicketsFromHtml(htmlContent);
                
                foreach (var ticket in tickets)
                {
                    // Check if ticket already exists
                    if (!_tickets.Any(t => t.Key == ticket.Key))
                    {
                        TicketDatabase.AddTicket(ticket);
                        _tickets.Add(ticket);
                    }
                }
                
                UpdateStatusMessage($"Loaded {tickets.Count} tickets from start.html");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading from HTML: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateHtml_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Use path from settings instead of output directory
                var htmlFilePath = _appSettings?.StartHtmlPath ?? @"D:\MyApps\Extensions\StartExtension\start.html";
                
                // Debug: Show which path is being used
                Console.WriteLine($"Using HTML path: {htmlFilePath}");
                Console.WriteLine($"Settings loaded: {_appSettings != null}");
                if (_appSettings != null)
                {
                    Console.WriteLine($"StartHtmlPath from settings: {_appSettings.StartHtmlPath}");
                }
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(htmlFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"Created directory: {directory}");
                }

                var htmlContent = GenerateHtmlContent();
                
                File.WriteAllText(htmlFilePath, htmlContent);
                UpdateStatusMessage($"Generated start.html with {_tickets.Count} tickets at {htmlFilePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating HTML: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TicketsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteSelectedBtn.IsEnabled = TicketsList.SelectedItem != null;
        }

        private void TicketsList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && TicketsList.SelectedItem != null)
            {
                DeleteSelectedTicket_Click(sender, e);
            }
        }

        private void TicketsList_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Changes are automatically saved to database when tickets are modified
        }

        private void OpenTicket_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string url && !string.IsNullOrEmpty(url))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ProcessJiraUrl()
        {
            var url = JiraUrlBox.Text.Trim();
            Console.WriteLine($"=== ProcessJiraUrl called with URL: {url} ===");
            
            if (string.IsNullOrEmpty(url))
            {
                Console.WriteLine("URL is empty, returning");
                return;
            }

            // Extract ticket information from URL
            var match = Regex.Match(url, @"([A-Z]+-\d+)");
            if (match.Success)
            {
                var ticketKey = match.Groups[1].Value;
                Console.WriteLine($"Extracted ticket key: {ticketKey}");
                
                // Try to get the real summary if Jira service is available
                if (_jiraService != null)
                {
                    Console.WriteLine("Jira service is available, fetching details...");
                    try
                    {
                        UpdateStatusMessage($"Fetching details for {ticketKey}...");
                        
                        // Use the single issue API directly
                        Console.WriteLine($"Fetching issue details for: {ticketKey}");
                        var result = await _jiraService.GetIssueSummaryDetailedAsync(ticketKey);
                        Console.WriteLine($"API result - Success: {result.Success}, Summary: {result.Summary}, Status: {result.Status}, Type: {result.Type}, Priority: {result.Priority}, Error: {result.ErrorMessage}");
                        
                        if (result.Success && !string.IsNullOrEmpty(result.Summary))
                        {
                            Console.WriteLine($"Found ticket details: {result.Summary}");
                            TicketNameBox.Text = result.Summary;
                            
                            // Set Status if available and found in ComboBox
                            if (!string.IsNullOrEmpty(result.Status))
                            {
                                var statusItem = StatusBox.Items.Cast<ComboBoxItem>()
                                    .FirstOrDefault(item => item.Content.ToString().Equals(result.Status, StringComparison.OrdinalIgnoreCase));
                                if (statusItem != null)
                                {
                                    StatusBox.SelectedItem = statusItem;
                                    Console.WriteLine($"Set status to: {result.Status}");
                                }
                                else
                                {
                                    Console.WriteLine($"Status '{result.Status}' not found in ComboBox");
                                }
                            }
                            
                            // Set Type if available and found in ComboBox
                            if (!string.IsNullOrEmpty(result.Type))
                            {
                                var typeItem = TypeBox.Items.Cast<ComboBoxItem>()
                                    .FirstOrDefault(item => item.Content.ToString().Equals(result.Type, StringComparison.OrdinalIgnoreCase));
                                if (typeItem != null)
                                {
                                    TypeBox.SelectedItem = typeItem;
                                    Console.WriteLine($"Set type to: {result.Type}");
                                }
                                else
                                {
                                    Console.WriteLine($"Type '{result.Type}' not found in ComboBox");
                                }
                            }
                            
                            // Set Priority if available and found in ComboBox
                            if (!string.IsNullOrEmpty(result.Priority))
                            {
                                var priorityItem = PriorityBox.Items.Cast<ComboBoxItem>()
                                    .FirstOrDefault(item => item.Content.ToString().Equals(result.Priority, StringComparison.OrdinalIgnoreCase));
                                if (priorityItem != null)
                                {
                                    PriorityBox.SelectedItem = priorityItem;
                                    Console.WriteLine($"Set priority to: {result.Priority}");
                                }
                                else
                                {
                                    Console.WriteLine($"Priority '{result.Priority}' not found in ComboBox");
                                }
                            }
                            
                            UpdateStatusMessage($"Loaded: {ticketKey} - {result.Summary} [{result.Type}] [{result.Priority}] [{result.Status}]");
                        }
                        else
                        {
                            Console.WriteLine($"No summary found or API failed: {result.ErrorMessage}");
                            TicketNameBox.Text = ticketKey;
                            UpdateStatusMessage($"Using ticket key as name: {ticketKey}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception in ProcessJiraUrl: {ex.Message}");
                        TicketNameBox.Text = ticketKey;
                        UpdateStatusMessage($"Using ticket key as name: {ticketKey}");
                    }
                }
                else
                {
                    Console.WriteLine("Jira service is null");
                    TicketNameBox.Text = ticketKey;
                    UpdateStatusMessage($"Using ticket key as name: {ticketKey}");
                }
                
                AddTicketBtn.IsEnabled = StatusBox.SelectedItem != null;
            }
            else
            {
                Console.WriteLine("No ticket key found in URL");
            }
        }

        private void LoadSettings()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    _appSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    _appSettings = new AppSettings();
                }
            }
            catch
            {
                _appSettings = new AppSettings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                var json = JsonSerializer.Serialize(_appSettings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTickets()
        {
            try
            {
                _tickets.Clear();
                var tickets = TicketDatabase.LoadTickets();
                foreach (var ticket in tickets)
                {
                    _tickets.Add(ticket);
                }
                UpdateStatusMessage($"Loaded {tickets.Count} tickets from database");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tickets: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task TryLoadTicketsFromJiraOrFallbackToManual()
        {
            Console.WriteLine("=== TryLoadTicketsFromJiraOrFallbackToManual called ===");
            if (_jiraService == null)
            {
                Console.WriteLine("Jira service is null");
                UpdateStatusMessage("Jira not configured - falling back to manual mode");
                ManualModeBox.IsChecked = true;
                _appSettings.Mode = "manual";
                SaveSettings();
                LoadTickets();
                return;
            }

            try
            {
                Console.WriteLine("Calling LoadTicketsFromJiraAsync...");
                UpdateStatusMessage("Loading tickets from Jira...");
                await LoadTicketsFromJiraAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Jira error: {ex.Message}");
                UpdateStatusMessage($"Jira connection failed - falling back to manual mode: {ex.Message}");
                ManualModeBox.IsChecked = true;
                _appSettings.Mode = "manual";
                SaveSettings();
                LoadTickets();
            }
        }

        private async Task LoadTicketsFromJiraAsync()
        {
            if (_jiraService == null)
            {
                throw new InvalidOperationException("Jira service not initialized");
            }

            try
            {
                var jiraTickets = await _jiraService.GetAllTicketsAsync();
                
                // Load existing tickets once for performance
                var existingTickets = TicketDatabase.LoadTickets().ToDictionary(t => t.Key, t => t);
                
                int imported = 0;
                int updated = 0;
                
                foreach (var jiraTicket in jiraTickets)
                {
                    var ticket = new Ticket
                    {
                        Key = jiraTicket.Key,
                        Summary = jiraTicket.Summary,
                        Status = jiraTicket.Status,
                        Url = jiraTicket.Url,
                        Type = jiraTicket.Type,
                        Priority = jiraTicket.Priority
                    };

                    // Check if ticket exists in database
                    if (existingTickets.TryGetValue(ticket.Key, out var existingTicket))
                    {
                        // Check if ticket needs updating
                        if (existingTicket.Summary != ticket.Summary ||
                            existingTicket.Status != ticket.Status ||
                            existingTicket.Type != ticket.Type ||
                            existingTicket.Priority != ticket.Priority)
                        {
                            TicketDatabase.UpdateTicket(ticket);
                            updated++;
                        }
                    }
                    else
                    {
                        // Add new ticket
                        TicketDatabase.AddTicket(ticket);
                        imported++;
                    }
                }

                // Reload tickets from database to refresh UI
                LoadTickets();

                UpdateStatusMessage($"Loaded {jiraTickets.Count} tickets from Jira: {imported} new, {updated} updated");
            }
            catch (Exception ex)
            {
                UpdateStatusMessage($"Error loading from Jira: {ex.Message}");
                throw;
            }
        }

        private void UpdateStatusMessage(string message)
        {
            StatusMessage.Text = message;
            StatusMessage.Foreground = System.Windows.Media.Brushes.Green;
        }

        private List<Ticket> ParseTicketsFromHtml(string htmlContent)
        {
            var tickets = new List<Ticket>();
            
            // Parse HTML table rows to extract ticket information
            var rowPattern = @"<tr[^>]*>.*?</tr>";
            var cellPattern = @"<td[^>]*>(.*?)</td>";
            var linkPattern = @"<a[^>]*href=""([^""]*)"">([^<]*)</a>";
            
            var rowMatches = Regex.Matches(htmlContent, rowPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            
            foreach (Match rowMatch in rowMatches)
            {
                var cellMatches = Regex.Matches(rowMatch.Value, cellPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                
                if (cellMatches.Count >= 3) // Expecting at least 3 cells: Key, Summary, Status
                {
                    var keyCell = cellMatches[0].Groups[1].Value;
                    var summaryCell = cellMatches[1].Groups[1].Value;
                    var statusCell = cellMatches[2].Groups[1].Value;
                    
                    // Extract link from key cell if present
                    var linkMatch = Regex.Match(keyCell, linkPattern);
                    var key = linkMatch.Success ? linkMatch.Groups[2].Value.Trim() : Regex.Replace(keyCell, @"<[^>]*>", "").Trim();
                    var url = linkMatch.Success ? linkMatch.Groups[1].Value : "";
                    
                    var summary = Regex.Replace(summaryCell, @"<[^>]*>", "").Trim();
                    var status = Regex.Replace(statusCell, @"<[^>]*>", "").Trim();
                    
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(summary))
                    {
                        tickets.Add(new Ticket
                        {
                            Key = key,
                            Summary = summary,
                            Status = status,
                            Url = url
                        });
                    }
                }
            }
            
            return tickets;
        }

        private string GenerateHtmlContent()
        {
            var header = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>My Jobs - Ticket Status</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            margin: 0;
            padding: 20px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            border-radius: 15px;
            box-shadow: 0 20px 40px rgba(0,0,0,0.1);
            overflow: hidden;
        }
        .header {
            background: linear-gradient(45deg, #2196F3, #21CBF3);
            color: white;
            padding: 30px;
            text-align: center;
        }
        .header h1 {
            margin: 0;
            font-size: 2.5em;
            font-weight: 300;
        }
        .stats {
            padding: 20px 30px;
            background: #f8f9fa;
            display: flex;
            justify-content: space-around;
            flex-wrap: wrap;
        }
        .stat-item {
            text-align: center;
            margin: 10px;
        }
        .stat-number {
            font-size: 2em;
            font-weight: bold;
            color: #2196F3;
        }
        .stat-label {
            color: #666;
            font-size: 0.9em;
        }
        .tickets-table {
            width: 100%;
            border-collapse: collapse;
            margin: 0;
        }
        .tickets-table th {
            background: #2196F3;
            color: white;
            padding: 15px;
            text-align: left;
            font-weight: 500;
        }
        .tickets-table td {
            padding: 12px 15px;
            border-bottom: 1px solid #eee;
            vertical-align: middle;
        }
        .tickets-table td:first-child {
            white-space: nowrap;
        }
        .tickets-table tr:hover {
            background: #f8f9fa;
        }
        .ticket-key {
            font-weight: bold;
            color: #2196F3;
            text-decoration: none;
        }
        .ticket-key:hover {
            text-decoration: underline;
        }
        .status-badge {
            padding: 6px 12px;
            border-radius: 20px;
            font-size: 0.85em;
            font-weight: 500;
            text-align: center;
            min-width: 100px;
            display: inline-block;
        }
        .status-todo { background: #ffd700; color: #000; }
        .status-refinement { background: #e3f2fd; color: #1976d2; }
        .status-in-progress { background: #0c9073; color: #fff; }
        .status-waiting-for-deploy { background: #726e6e; color: #fff; }
        .status-in-test { background: #85c50e; color: #000; }
        .status-test-succeeded { background: #0c9073; color: #ffff00; }
        .status-test-failed { background: #f44336; color: #fff; }
        .status-done { background: transparent; color: #4caf50; font-weight: bold; }
        .status-canceled { background: #9e9e9e; color: #fff; }
        .status-on-hold { background: #3b02f7; color: #fbff1c; }
        .footer {
            padding: 20px;
            text-align: center;
            color: #666;
            font-size: 0.9em;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🎯 My Jobs - Ticket Status</h1>
            <p>Last updated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"</p>
        </div>
        
        <div class=""stats"">
            <div class=""stat-item"">
                <div class=""stat-number"">" + _tickets.Count + @"</div>
                <div class=""stat-label"">Total Tickets</div>
            </div>
            <div class=""stat-item"">
                <div class=""stat-number"">" + _tickets.Count(t => t.Status == "In Progress") + @"</div>
                <div class=""stat-label"">In Progress</div>
            </div>
            <div class=""stat-item"">
                <div class=""stat-number"">" + _tickets.Count(t => t.Status == "Done") + @"</div>
                <div class=""stat-label"">Done</div>
            </div>
        </div>";
        var ticket_table = @"
        <table class=""tickets-table"">
            <thead>
                <tr>
                    <th>Ticket</th>
                    <th>Summary</th>
                    <th>Status</th>
                </tr>
            </thead>
            <tbody>";

            foreach (var ticket in _tickets.OrderBy(t => t.Key))
            {
                var statusClass = GetStatusCssClass(ticket.Status);
                var ticketLink = !string.IsNullOrEmpty(ticket.Url) ? 
                    $@"<a href=""{ticket.Url}"" class=""ticket-key"" target=""_blank"">{ticket.Key}</a>" : 
                    $@"<span class=""ticket-key"">{ticket.Key}</span>";

                ticket_table += $@"
                <tr>
                    <td>{ticketLink}</td>
                    <td>{ticket.Summary}</td>
                    <td><span class=""status-badge {statusClass}"">{ticket.Status}</span></td>
                </tr>";
            }

            ticket_table += @"
            </tbody>
        </table>";

        var footer = @"
        <div class=""footer"">
            Generated by MyJiraTickets Ticket Manager
        </div>
    </div>
</body>
</html>";

            string htmlPath = _appSettings.StartHtmlPath ?? @"D:\MyApps\Extensions\StartExtension\start.html";
            if (File.Exists(htmlPath))
            {
                string existingHtml = File.ReadAllText(htmlPath);
                string id = _appSettings.HtmlContentId;
                // Find the div with id="jira" and replace the table inside it
                string jiraDivPattern = $@"(<div[^>]*id=""{id}""[^>]*>.*?<table[^>]*class=""tickets-table""[^>]*>)(.*?)(</table>.*?</div>)";
                string replacement = "$1" + ticket_table + "$3";
                
                if (Regex.IsMatch(existingHtml, jiraDivPattern, RegexOptions.Singleline))
                {
                    string newHtml = Regex.Replace(existingHtml, jiraDivPattern, replacement, RegexOptions.Singleline);
                    return newHtml;
                }
                else
                {
                    // If no div with id="jira" found, return the full HTML
                    return header + ticket_table + footer;
                }
            }
            else
            {
                // If file doesn't exist, return the full HTML
                return header + ticket_table + footer;
            }
        }

        private string GetStatusCssClass(string status)
        {
            return status?.ToLower().Replace(" ", "-") switch
            {
                "to-do" => "status-todo",
                "refinement" => "status-refinement", 
                "in-progress" => "status-in-progress",
                "waiting-for-deploy" => "status-waiting-for-deploy",
                "in-test" => "status-in-test",
                "test-succeeded" => "status-test-succeeded",
                "test-failed" => "status-test-failed",
                "done" => "status-done",
                "canceled" => "status-canceled",
                "on-hold" => "status-on-hold",
                _ => ""
            };
        }

        private async void LoadAllTickets_Click(object sender, RoutedEventArgs e)
        {
            if (_jiraService == null)
            {
                SetStatusMessage("Jira service not configured. Please check settings.", System.Windows.Media.Brushes.Red);
                return;
            }

            try
            {
                // Show progress UI
                ProgressPanel.Visibility = Visibility.Visible;
                LoadAllTicketsBtn.IsEnabled = false;
                SetStatusMessage("Loading all tickets from Jira...", System.Windows.Media.Brushes.Blue);

                // Get all tickets with higher maxResults (no assignee filter for now)
                string jql = $"ORDER BY updated DESC";
                var jiraTickets = await _jiraService.GetAllTicketsAsync(jql, 1000);
                
                if (jiraTickets.Count == 0)
                {
                    SetStatusMessage("No tickets assigned to you found in Jira.", System.Windows.Media.Brushes.Orange);
                    return;
                }

                LoadingProgressBar.Maximum = jiraTickets.Count;
                LoadingProgressBar.Value = 0;
                ProgressText.Text = $"0/{jiraTickets.Count}";

                int imported = 0;
                int updated = 0;
                int skipped = 0;

                // Load existing tickets once to improve performance
                var existingTickets = TicketDatabase.LoadTickets().ToDictionary(t => t.Key, t => t);

                foreach (var jiraTicket in jiraTickets)
                {
                    if (existingTickets.TryGetValue(jiraTicket.Key, out var existingTicket))
                    {
                        // Check if ticket needs updating
                        if (existingTicket.Summary != jiraTicket.Summary ||
                            existingTicket.Status != jiraTicket.Status ||
                            existingTicket.Type != jiraTicket.Type ||
                            existingTicket.Priority != jiraTicket.Priority)
                        {
                            // Update existing ticket
                            var updatedTicket = new Ticket
                            {
                                Key = jiraTicket.Key,
                                Url = jiraTicket.Url,
                                Summary = jiraTicket.Summary,
                                Status = jiraTicket.Status,
                                Type = jiraTicket.Type,
                                Priority = jiraTicket.Priority
                            };

                            TicketDatabase.UpdateTicket(updatedTicket);
                            updated++;
                        }
                        else
                        {
                            skipped++;
                        }
                    }
                    else
                    {
                        // Create new ticket and add to database
                        var ticket = new Ticket
                        {
                            Key = jiraTicket.Key,
                            Url = jiraTicket.Url,
                            Summary = jiraTicket.Summary,
                            Status = jiraTicket.Status,
                            Type = jiraTicket.Type,
                            Priority = jiraTicket.Priority
                        };

                        TicketDatabase.AddTicket(ticket);
                        imported++;
                    }

                    // Update progress
                    LoadingProgressBar.Value++;
                    ProgressText.Text = $"{LoadingProgressBar.Value}/{jiraTickets.Count}";
                    
                    // Allow UI to update every 10 tickets to improve performance
                    if (LoadingProgressBar.Value % 10 == 0)
                    {
                        await Task.Delay(1);
                    }
                }

                // Refresh the ticket list
                LoadTickets();

                SetStatusMessage($"Import completed! {imported} new, {updated} updated, {skipped} unchanged.", System.Windows.Media.Brushes.Green);
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Error loading tickets: {ex.Message}", System.Windows.Media.Brushes.Red);
            }
            finally
            {
                // Hide progress UI
                ProgressPanel.Visibility = Visibility.Collapsed;
                LoadAllTicketsBtn.IsEnabled = true;
            }
        }

        private void SetStatusMessage(string message, System.Windows.Media.Brush color)
        {
            StatusMessage.Text = message;
            StatusMessage.Foreground = color;
        }
    }
}
