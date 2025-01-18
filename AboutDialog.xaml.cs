using System;
using System.Diagnostics;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace EBDC
{
    public partial class AboutDialog : Window
    {
        public AboutDialog()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DonateButton_Click(object sender, RoutedEventArgs e)
        {
            var paypalUrl = "https://www.paypal.com/donate?hosted_button_id=783XTZLS4JB7U";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = paypalUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nell'apertura del link PayPal: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
