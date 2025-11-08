using System;
using System.IO;
using System.Text;
using System.Windows;

namespace Trident.MITM
{
    /// <summary>
    /// Global exception handler and logger for ARTHEMIS CONTROL
    /// </summary>
    public static class ExceptionHandler
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ArthemisControl", "Logs");
        
        private static readonly string LogFile = Path.Combine(LogDirectory, 
            $"arthemis_{DateTime.Now:yyyyMMdd}.log");

        static ExceptionHandler()
        {
            // Ensure log directory exists
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }

        /// <summary>
        /// Logs an exception with full details
        /// </summary>
        public static void LogException(Exception ex, string context = "")
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] EXCEPTION");
                
                if (!string.IsNullOrEmpty(context))
                {
                    sb.AppendLine($"Context: {context}");
                }
                
                sb.AppendLine($"Type: {ex.GetType().FullName}");
                sb.AppendLine($"Message: {ex.Message}");
                sb.AppendLine($"Stack Trace:\n{ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    sb.AppendLine("\nInner Exception:");
                    sb.AppendLine($"Type: {ex.InnerException.GetType().FullName}");
                    sb.AppendLine($"Message: {ex.InnerException.Message}");
                    sb.AppendLine($"Stack Trace:\n{ex.InnerException.StackTrace}");
                }
                
                sb.AppendLine(new string('-', 80));
                sb.AppendLine();
                
                File.AppendAllText(LogFile, sb.ToString());
            }
            catch
            {
                // Silent fail - don't crash while handling crashes
            }
        }

        /// <summary>
        /// Logs an informational message
        /// </summary>
        public static void LogInfo(string message)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] INFO: {message}\n";
                File.AppendAllText(LogFile, logMessage);
            }
            catch
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        public static void LogWarning(string message)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] WARNING: {message}\n";
                File.AppendAllText(LogFile, logMessage);
            }
            catch
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        public static void LogError(string message)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ERROR: {message}\n";
                File.AppendAllText(LogFile, logMessage);
            }
            catch
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Shows a user-friendly error dialog
        /// </summary>
        public static void ShowUserError(string message, string title = "Error")
        {
            try
            {
                MessageBox.Show(
                    message,
                    $"ARTHEMIS CONTROL - {title}",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Shows a user-friendly warning dialog
        /// </summary>
        public static void ShowUserWarning(string message, string title = "Warning")
        {
            try
            {
                MessageBox.Show(
                    message,
                    $"ARTHEMIS CONTROL - {title}",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Shows a user-friendly information dialog
        /// </summary>
        public static void ShowUserInfo(string message, string title = "Information")
        {
            try
            {
                MessageBox.Show(
                    message,
                    $"ARTHEMIS CONTROL - {title}",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Handles an exception with logging and optional user notification
        /// </summary>
        public static void Handle(Exception ex, string context = "", bool showToUser = false)
        {
            LogException(ex, context);
            
            if (showToUser)
            {
                var userMessage = GetUserFriendlyMessage(ex);
                ShowUserError(userMessage, "An Error Occurred");
            }
        }

        /// <summary>
        /// Converts technical exception to user-friendly message
        /// </summary>
        private static string GetUserFriendlyMessage(Exception ex)
        {
            return ex switch
            {
                FileNotFoundException => "A required file could not be found. Please reinstall the application.",
                UnauthorizedAccessException => "Permission denied. Please run the application as Administrator.",
                System.Net.NetworkInformation.PingException => "Network connection error. Please check your internet connection.",
                TimeoutException => "The operation timed out. Please try again.",
                InvalidOperationException => "Invalid operation. Please check your configuration.",
                ArgumentException => "Invalid input. Please check your settings.",
                _ => "An unexpected error occurred. Please check the log file for more details.\n\nIf this problem persists, contact support."
            };
        }

        /// <summary>
        /// Opens the log folder in Windows Explorer
        /// </summary>
        public static void OpenLogFolder()
        {
            try
            {
                if (Directory.Exists(LogDirectory))
                {
                    System.Diagnostics.Process.Start("explorer.exe", LogDirectory);
                }
            }
            catch (Exception ex)
            {
                LogException(ex, "OpenLogFolder");
            }
        }

        /// <summary>
        /// Cleans old log files (keeps last 30 days)
        /// </summary>
        public static void CleanOldLogs(int daysToKeep = 30)
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    return;

                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var files = Directory.GetFiles(LogDirectory, "arthemis_*.log");

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex, "CleanOldLogs");
            }
        }

        /// <summary>
        /// Gets the current log file path
        /// </summary>
        public static string GetLogFilePath()
        {
            return LogFile;
        }

        /// <summary>
        /// Gets the log directory path
        /// </summary>
        public static string GetLogDirectory()
        {
            return LogDirectory;
        }
    }
}
