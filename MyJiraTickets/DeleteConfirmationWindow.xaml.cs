using System.Windows;
using System.Windows.Controls;
using MyJiraTickets.Models;

namespace MyJiraTickets
{
    public partial class DeleteConfirmationWindow : Window
    {
        private readonly Ticket _ticket;
        public bool Confirmed { get; private set; }

        public DeleteConfirmationWindow(Ticket ticket)
        {
            InitializeComponent();
            _ticket = ticket;
            SetupWindow();
        }

        private void SetupWindow()
        {
            // Display ticket information
            TicketInfoText.Text = $"Key: {_ticket.Key}\nSummary: {_ticket.Summary}\nStatus: {_ticket.Status}";
            
            // Update the hint text to show the exact ticket key
            HintTextBlock.Text = $"Ukucajte: {_ticket.Key} (case sensitive!)";
            
            // Focus on text box
            ConfirmationTextBox.Focus();
        }

        private void ConfirmationTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var enteredText = ConfirmationTextBox.Text;
            var isMatch = enteredText == _ticket.Key;

            // Enable/disable delete button based on exact match
            DeleteButton.IsEnabled = isMatch;

            // Show error message if text is entered but doesn't match
            if (!string.IsNullOrEmpty(enteredText) && !isMatch)
            {
                ErrorMessage.Text = "Ticket key does not match. Please type it exactly as shown above.";
                ErrorMessage.Visibility = Visibility.Visible;
                DeleteButton.Background = System.Windows.Media.Brushes.Gray;
            }
            else
            {
                ErrorMessage.Visibility = Visibility.Collapsed;
                DeleteButton.Background = isMatch ? 
                    System.Windows.Media.Brushes.DarkRed : 
                    System.Windows.Media.Brushes.Gray;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Double check that the text matches before confirming
            if (ConfirmationTextBox.Text == _ticket.Key)
            {
                Confirmed = true;
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogResult = false;
            Close();
        }

        // Handle Enter key to confirm if text matches
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && DeleteButton.IsEnabled)
            {
                DeleteButton_Click(sender, e);
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                CancelButton_Click(sender, e);
            }
        }
    }
}
