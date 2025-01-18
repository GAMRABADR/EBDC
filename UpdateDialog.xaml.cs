using System;
using System.Windows;
using EBDC.Services;

namespace EBDC
{
    public partial class UpdateDialog : Window
    {
        private readonly UpdateService _updateService;
        private string _newVersion;

        public UpdateDialog(UpdateService updateService, string newVersion)
        {
            InitializeComponent();
            _updateService = updateService;
            _newVersion = newVersion;

            VersionText.Text = $"Versione disponibile: {newVersion}";
            _updateService.DownloadProgressChanged += OnDownloadProgressChanged;
            _updateService.UpdateCompleted += OnUpdateCompleted;
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            UpdateProgress.Visibility = Visibility.Visible;
            StatusText.Text = "Download in corso...";

            _updateService.DownloadAndInstallUpdateAsync(_newVersion);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnDownloadProgressChanged(double progress)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateProgress.Value = progress;
                StatusText.Text = $"Download in corso: {progress:F1}%";
            });
        }

        private void OnUpdateCompleted(bool success, string message)
        {
            Dispatcher.Invoke(() =>
            {
                if (success)
                {
                    StatusText.Text = "Aggiornamento completato. L'applicazione verrà riavviata...";
                    Close();
                }
                else
                {
                    StatusText.Text = message;
                    UpdateButton.IsEnabled = true;
                    CancelButton.IsEnabled = true;
                }
            });
        }
    }
}
