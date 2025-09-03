using System.Windows;
using System.Windows.Controls;

namespace MyJiraTickets;

public partial class SettingsWindow : Window
{
    private Models.AppSettings? _settings;

    public SettingsWindow()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        _settings = Models.AppSettings.Load("appsettings.json");
        if (_settings != null)
        {
            var baseUrlBox = (TextBox)FindName("BaseUrlBox");
            var usernameBox = (TextBox)FindName("UsernameBox");
            var passwordBox = (PasswordBox)FindName("PasswordBox");
            var apiKeyBox = (TextBox)FindName("ApiKeyBox");
            var htmlPathBox = (TextBox)FindName("HtmlPathBox");

            if (passwordBox != null) passwordBox.Password = _settings.Jira.Password;
            if (apiKeyBox != null) apiKeyBox.Text = _settings.Jira.ApiKey;
            if (htmlPathBox != null) htmlPathBox.Text = _settings.StartHtmlPath;
            if (baseUrlBox != null) baseUrlBox.Text = _settings.Jira.BaseUrl;
            if (usernameBox != null) usernameBox.Text = _settings.Jira.Username;

            if (string.IsNullOrWhiteSpace(BaseUrlBox.Text) && string.IsNullOrWhiteSpace(UsernameBox.Text))
            {
                // Dijagnostika - obavesti korisnika da fajl možda nije pronađen
                // (Može se ukloniti kasnije)
                MessageBox.Show("Podešavanja nisu učitana (appsettings.json možda nije pronađen).", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_settings == null)
            _settings = new Models.AppSettings();

    var baseUrlBox2 = (TextBox)FindName("BaseUrlBox");
    var usernameBox2 = (TextBox)FindName("UsernameBox");
    var passwordBox2 = (PasswordBox)FindName("PasswordBox");
    var apiKeyBox2 = (TextBox)FindName("ApiKeyBox");
    var htmlPathBox2 = (TextBox)FindName("HtmlPathBox");

    if (baseUrlBox2 != null) _settings.Jira.BaseUrl = baseUrlBox2.Text.Trim();
    if (usernameBox2 != null) _settings.Jira.Username = usernameBox2.Text.Trim();
    if (passwordBox2 != null) _settings.Jira.Password = passwordBox2.Password;
    if (apiKeyBox2 != null) _settings.Jira.ApiKey = apiKeyBox2.Text.Trim();
    if (htmlPathBox2 != null) _settings.StartHtmlPath = htmlPathBox2.Text.Trim();
        _settings.Save("appsettings.json");
        this.Close();
    }
}
