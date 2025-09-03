using System.Windows;
using System.Windows.Controls;

namespace MyJiraTickets
{
    public partial class EditTicketWindow : Window
    {
        public Models.Ticket Ticket { get; private set; }

        public EditTicketWindow(Models.Ticket ticket)
        {
            InitializeComponent();
            Ticket = ticket;
            LoadTicketData();
        }

        private void LoadTicketData()
        {
            KeyBox.Text = Ticket.Key;
            UrlBox.Text = Ticket.Url;
            SummaryBox.Text = Ticket.Summary;
            
            // Set status
            foreach (System.Windows.Controls.ComboBoxItem item in StatusBox.Items)
            {
                if (item.Content.ToString() == Ticket.Status)
                {
                    StatusBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(UrlBox.Text))
            {
                MessageBox.Show("URL is required!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(SummaryBox.Text))
            {
                MessageBox.Show("Summary is required!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save changes
            Ticket.Url = UrlBox.Text.Trim();
            Ticket.Summary = SummaryBox.Text.Trim();
            
            // Save status
            if (StatusBox.SelectedItem is ComboBoxItem selectedItem)
            {
                Ticket.Status = selectedItem.Content?.ToString() ?? "";
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
