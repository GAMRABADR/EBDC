using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using EBDC.Commands;
using EBDC.Models;
using EBDC.Services;
using EBDC.Services.Interfaces;
using Forms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using Microsoft.Extensions.Logging;

namespace EBDC
{
    public partial class MainWindow : Window
    {
        private readonly IDownloadManager _downloadManager;
        private readonly ISystemMonitor _systemMonitor;
        private readonly PersistentSettingsService _settingsService;
        private readonly UpdateService _updateService;
        private readonly ILogger<MainWindow> _logger;
        private readonly ObservableCollection<VideoInfo> _videos;
        private readonly SemaphoreSlim _downloadSemaphore;
        private const int MaxConcurrentDownloads = 3;
        private int _activeDownloads;

        public MainWindow(IDownloadManager downloadManager, ISystemMonitor systemMonitor, 
                         UpdateService updateService, ILogger<MainWindow> logger)
        {
            InitializeComponent();
            _downloadManager = downloadManager;
            _systemMonitor = systemMonitor;
            _updateService = updateService;
            _logger = logger;
            _settingsService = new PersistentSettingsService();
            _videos = new ObservableCollection<VideoInfo>();
            _downloadSemaphore = new SemaphoreSlim(MaxConcurrentDownloads);
            _activeDownloads = 0;

            VideoList.ItemsSource = _videos;
            DownloadAllButton.IsEnabled = false;

            _downloadManager.DownloadProgressChanged += OnDownloadProgressChanged;
            _downloadManager.DownloadCompleted += OnDownloadCompleted;
            _downloadManager.DownloadFailed += OnDownloadFailed;
            _systemMonitor.ResourcesUpdated += OnResourcesUpdated;
            _updateService.UpdateAvailable += OnUpdateAvailable;
            _systemMonitor.Start();

            LoadSavedSettings();
            
            // Controllo automatico degli aggiornamenti all'avvio
            CheckForUpdatesAsync();
        }

        private void LoadSavedSettings()
        {
            if (!string.IsNullOrEmpty(_settingsService.CurrentSettings.DownloadPath))
            {
                _downloadManager.SetDownloadPath(_settingsService.CurrentSettings.DownloadPath);
                UpdateDownloadPathText();
            }

            var formatItem = FormatComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Content?.ToString() == _settingsService.CurrentSettings.LastSelectedFormat);
            if (formatItem != null)
            {
                FormatComboBox.SelectedItem = formatItem;
            }
            else
            {
                FormatComboBox.SelectedIndex = 0;
            }

            var qualityItem = QualityComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Content?.ToString() == _settingsService.CurrentSettings.LastSelectedQuality);
            if (qualityItem != null)
            {
                QualityComboBox.SelectedItem = qualityItem;
            }
            else
            {
                QualityComboBox.SelectedIndex = 0;
            }

            UpdateQualityComboBoxState();
        }

        private async void ExtractButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var urls = UrlTextBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(url => url.Trim())
                    .Where(url => !string.IsNullOrEmpty(url))
                    .ToList();

                if (!urls.Any())
                {
                    MessageBox.Show("Inserisci almeno un URL valido.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ExtractButton.IsEnabled = false;
                _videos.Clear();

                foreach (var url in urls)
                {
                    try
                    {
                        var videoInfo = await _downloadManager.GetVideoInfoAsync(url);
                        if (videoInfo.Status != DownloadStatus.Error)
                        {
                            videoInfo.Format = ((ComboBoxItem)FormatComboBox.SelectedItem).Content.ToString() ?? string.Empty;
                            videoInfo.Quality = ((ComboBoxItem)QualityComboBox.SelectedItem).Content.ToString() ?? string.Empty;
                            videoInfo.DownloadCommand = new RelayCommand(_ => StartDownload(videoInfo), _ => !videoInfo.IsDownloading);
                            videoInfo.CancelCommand = new RelayCommand(_ => CancelDownload(videoInfo), _ => videoInfo.IsDownloading);
                            _videos.Add(videoInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Errore nell'elaborazione dell'URL {url}: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                if (_videos.Any())
                {
                    DownloadAllButton.IsEnabled = true;
                }
            }
            finally
            {
                ExtractButton.IsEnabled = true;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            UrlTextBox.Clear();
            _videos.Clear();
            DownloadAllButton.IsEnabled = false;
        }

        private void FormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FormatComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content != null)
            {
                var format = selectedItem.Content.ToString();
                if (!string.IsNullOrEmpty(format))
                {
                    _settingsService.UpdateLastSelectedFormat(format);
                }
                UpdateQualityComboBoxState();
            }
        }

        private void UpdateQualityComboBoxState()
        {
            if (FormatComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var isVideo = selectedItem.Content.ToString()?.Contains("Video") ?? false;
                QualityComboBox.IsEnabled = isVideo;
                if (!isVideo)
                {
                    QualityComboBox.SelectedIndex = 0;
                }
            }
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new Forms.FolderBrowserDialog
            {
                Description = "Seleziona la cartella di download",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                _downloadManager.SetDownloadPath(dialog.SelectedPath);
                _settingsService.UpdateDownloadPath(dialog.SelectedPath);
                UpdateDownloadPathText();
            }
        }

        private void UpdateDownloadPathText()
        {
            DownloadPathText.Text = _settingsService.CurrentSettings.DownloadPath;
        }

        private async void DownloadAllButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedVideos = _videos.Where(v => v.IsSelected && !v.IsDownloading).ToList();
            if (!selectedVideos.Any())
            {
                MessageBox.Show("Seleziona almeno un video da scaricare.", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var video in selectedVideos)
            {
                await StartDownload(video);
            }
        }

        private async Task StartDownload(VideoInfo video)
        {
            if (string.IsNullOrEmpty(_settingsService.CurrentSettings.DownloadPath))
            {
                MessageBox.Show("Seleziona prima una cartella di download.", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _downloadSemaphore.WaitAsync();
                Interlocked.Increment(ref _activeDownloads);
                UpdateActiveDownloadsText();

                video.Status = DownloadStatus.Downloading;
                await _downloadManager.StartDownloadAsync(video);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante il download: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                video.Status = DownloadStatus.Error;
            }
            finally
            {
                _downloadSemaphore.Release();
                Interlocked.Decrement(ref _activeDownloads);
                UpdateActiveDownloadsText();
            }
        }

        private void CancelDownload(VideoInfo video)
        {
            _downloadManager.CancelDownload(video.Url);
        }

        private void OnDownloadProgressChanged(VideoInfo video)
        {
            Dispatcher.Invoke(() =>
            {
                var existingVideo = _videos.FirstOrDefault(v => v.Url == video.Url);
                if (existingVideo != null)
                {
                    existingVideo.Progress = video.Progress;
                    existingVideo.Status = video.Status;
                }
            });
        }

        private void OnDownloadCompleted(VideoInfo video)
        {
            Dispatcher.Invoke(() =>
            {
                var existingVideo = _videos.FirstOrDefault(v => v.Url == video.Url);
                if (existingVideo != null)
                {
                    existingVideo.Status = DownloadStatus.Completed;
                    existingVideo.Progress = 100;
                }
            });
        }

        private void OnDownloadFailed(VideoInfo video, Exception error)
        {
            Dispatcher.Invoke(() =>
            {
                var existingVideo = _videos.FirstOrDefault(v => v.Url == video.Url);
                if (existingVideo != null)
                {
                    existingVideo.Status = DownloadStatus.Error;
                }
                MessageBox.Show($"Errore durante il download di {video.Title}: {error.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private void OnResourcesUpdated(ResourcesInfo info)
        {
            Dispatcher.Invoke(() =>
            {
                MemoryUsageText.Text = $"RAM: {info.MemoryUsageMB:F1} MB";
            });
        }

        private void UpdateActiveDownloadsText()
        {
            Dispatcher.Invoke(() =>
            {
                ActiveDownloadsText.Text = $"Download Attivi: {_activeDownloads}";
            });
        }

        private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            await CheckForUpdatesAsync(true);
        }

        private async Task CheckForUpdatesAsync(bool showNoUpdatesMessage = false)
        {
            try
            {
                bool updateFound = false;
                _updateService.UpdateAvailable += (version) => updateFound = true;
                
                await _updateService.CheckForUpdatesAsync();
                
                if (!updateFound && showNoUpdatesMessage)
                {
                    MessageBox.Show("Nessun aggiornamento disponibile.", "Aggiornamenti",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il controllo degli aggiornamenti");
                if (showNoUpdatesMessage)
                {
                    MessageBox.Show("Errore durante il controllo degli aggiornamenti.", "Errore",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnUpdateAvailable(string version)
        {
            Dispatcher.Invoke(() =>
            {
                var dialog = new UpdateDialog(_updateService, version);
                dialog.Owner = this;
                dialog.ShowDialog();
            });
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var aboutDialog = new AboutDialog();
            aboutDialog.ShowDialog();
        }

        private void DonateMenuItem_Click(object sender, RoutedEventArgs e)
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
                _logger.LogError(ex, "Errore nell'apertura del link PayPal");
                MessageBox.Show($"Errore nell'apertura del link PayPal: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
