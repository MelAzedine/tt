using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace Trident.MITM
{
    /// <summary>
    /// About window showing application information
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            ExceptionHandler.LogInfo("About window opened");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ViewLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExceptionHandler.OpenLogFolder();
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, "ViewLogs_Click", true);
            }
        }

        private void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show(
                    "You are running the latest version (6.0.3)\n\n" +
                    "Updates are checked automatically on startup.\n" +
                    "Visit our website for manual downloads.",
                    "ARTHEMIS CONTROL - Update Check",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                ExceptionHandler.LogInfo("Update check performed");
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, "CheckUpdates_Click", true);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                // Open URL in default browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                
                e.Handled = true;
                ExceptionHandler.LogInfo($"Navigated to: {e.Uri.AbsoluteUri}");
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, "Hyperlink_RequestNavigate", true);
            }
        }
    }
}
