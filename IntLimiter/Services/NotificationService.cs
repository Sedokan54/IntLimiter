using System;
using System.Threading.Tasks;
using System.Windows;

namespace NetLimiterClone.Services
{
    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success
    }

    public class NotificationService
    {
        private bool _notificationsEnabled = true;

        public NotificationService()
        {
            // Simple notification service using MessageBox for now
        }

        public bool NotificationsEnabled
        {
            get => _notificationsEnabled;
            set => _notificationsEnabled = value;
        }

        public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
        {
            if (!_notificationsEnabled)
                return;

            try
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(message, title, MessageBoxButton.OK, GetMessageBoxImage(type));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show notification: {ex.Message}");
            }
        }

        public async Task ShowNotificationAsync(string title, string message, NotificationType type = NotificationType.Info)
        {
            await Task.Run(() => ShowNotification(title, message, type));
        }

        public void ShowBandwidthLimitReached(string processName, string limitType, string limitValue)
        {
            ShowNotification(
                "Bandwidth Limit Reached",
                $"{processName} has reached its {limitType} limit of {limitValue}",
                NotificationType.Warning
            );
        }

        public void ShowNewProcessDetected(string processName)
        {
            ShowNotification(
                "New Process Detected",
                $"New network process detected: {processName}",
                NotificationType.Info
            );
        }

        public void ShowHighUsageAlert(string processName, string usage)
        {
            ShowNotification(
                "High Network Usage",
                $"{processName} is using {usage} of bandwidth",
                NotificationType.Warning
            );
        }

        public void ShowProfileActivatedNotification(string profileName)
        {
            ShowNotification(
                "Profile Activated",
                $"Profile '{profileName}' has been activated",
                NotificationType.Info
            );
        }

        public void ShowBandwidthLimitNotification(string processName, string limitType, string limitValue)
        {
            ShowNotification(
                "Bandwidth Limit Applied",
                $"{limitType} limit of {limitValue} applied to {processName}",
                NotificationType.Info
            );
        }

        public void Dispose()
        {
            // Nothing to dispose for MessageBox-based notifications
        }

        private MessageBoxImage GetMessageBoxImage(NotificationType type)
        {
            return type switch
            {
                NotificationType.Error => MessageBoxImage.Error,
                NotificationType.Warning => MessageBoxImage.Warning,
                NotificationType.Success => MessageBoxImage.Information,
                NotificationType.Info => MessageBoxImage.Information,
                _ => MessageBoxImage.Information
            };
        }
    }
}