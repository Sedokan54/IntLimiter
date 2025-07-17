using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace NetLimiterClone.Services
{
    public class NotificationService
    {
        private const string APP_ID = "NetLimiterClone";
        private readonly ToastNotifier _toastNotifier;
        private bool _notificationsEnabled = true;

        public NotificationService()
        {
            try
            {
                _toastNotifier = ToastNotificationManager.CreateToastNotifier(APP_ID);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize toast notifier: {ex.Message}");
            }
        }

        public bool NotificationsEnabled
        {
            get => _notificationsEnabled;
            set => _notificationsEnabled = value;
        }

        public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
        {
            if (!_notificationsEnabled || _toastNotifier == null)
                return;

            try
            {
                var toastXml = CreateToastXml(title, message, type);
                var toast = new ToastNotification(toastXml);
                
                // Add event handlers
                toast.Activated += OnToastActivated;
                toast.Dismissed += OnToastDismissed;
                toast.Failed += OnToastFailed;

                _toastNotifier.Show(toast);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show toast notification: {ex.Message}");
            }
        }

        public void ShowBandwidthLimitNotification(string processName, long downloadLimit, long uploadLimit)
        {
            var message = $"Bandwidth limit applied to {processName}";
            if (downloadLimit > 0 && uploadLimit > 0)
            {
                message += $"\nDownload: {FormatBytes(downloadLimit)}/s, Upload: {FormatBytes(uploadLimit)}/s";
            }
            else if (downloadLimit > 0)
            {
                message += $"\nDownload: {FormatBytes(downloadLimit)}/s";
            }
            else if (uploadLimit > 0)
            {
                message += $"\nUpload: {FormatBytes(uploadLimit)}/s";
            }

            ShowNotification("Bandwidth Limit Applied", message, NotificationType.Success);
        }

        public void ShowProcessBlockedNotification(string processName)
        {
            ShowNotification("Process Blocked", 
                $"Network access blocked for {processName}", 
                NotificationType.Warning);
        }

        public void ShowHighUsageNotification(string processName, long downloadSpeed, long uploadSpeed)
        {
            var message = $"{processName} is using high bandwidth\n" +
                         $"Download: {FormatBytes(downloadSpeed)}/s, Upload: {FormatBytes(uploadSpeed)}/s";
            
            ShowNotification("High Network Usage", message, NotificationType.Warning);
        }

        public void ShowQuotaExceededNotification(string processName, long quotaLimit)
        {
            ShowNotification("Quota Exceeded", 
                $"{processName} has exceeded its quota limit of {FormatBytes(quotaLimit)}", 
                NotificationType.Error);
        }

        public void ShowProfileActivatedNotification(string profileName)
        {
            ShowNotification("Profile Activated", 
                $"Bandwidth profile '{profileName}' is now active", 
                NotificationType.Success);
        }

        public void ShowConnectionLimitNotification(string processName, int connectionCount, int limit)
        {
            ShowNotification("Connection Limit Reached", 
                $"{processName} has reached its connection limit ({connectionCount}/{limit})", 
                NotificationType.Warning);
        }

        private XmlDocument CreateToastXml(string title, string message, NotificationType type)
        {
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            
            // Set title
            var titleNodes = toastXml.GetElementsByTagName("text");
            if (titleNodes.Count > 0)
            {
                titleNodes[0].AppendChild(toastXml.CreateTextNode(title));
            }

            // Set message
            if (titleNodes.Count > 1)
            {
                titleNodes[1].AppendChild(toastXml.CreateTextNode(message));
            }

            // Add toast element attributes
            var toastNode = toastXml.SelectSingleNode("/toast");
            if (toastNode != null)
            {
                var toastElement = toastNode as XmlElement;
                toastElement?.SetAttribute("duration", "short");
                
                // Add audio based on notification type
                var audioElement = toastXml.CreateElement("audio");
                audioElement.SetAttribute("src", GetAudioForType(type));
                toastElement?.AppendChild(audioElement);

                // Add actions
                var actionsElement = toastXml.CreateElement("actions");
                
                // Show app action
                var showAction = toastXml.CreateElement("action");
                showAction.SetAttribute("content", "Show App");
                showAction.SetAttribute("arguments", "action=show");
                showAction.SetAttribute("activationType", "foreground");
                actionsElement.AppendChild(showAction);

                // Dismiss action
                var dismissAction = toastXml.CreateElement("action");
                dismissAction.SetAttribute("content", "Dismiss");
                dismissAction.SetAttribute("arguments", "action=dismiss");
                dismissAction.SetAttribute("activationType", "background");
                actionsElement.AppendChild(dismissAction);

                toastElement?.AppendChild(actionsElement);
            }

            return toastXml;
        }

        private string GetAudioForType(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => "ms-winsoundevent:Notification.Default",
                NotificationType.Warning => "ms-winsoundevent:Notification.Reminder",
                NotificationType.Error => "ms-winsoundevent:Notification.IM",
                _ => "ms-winsoundevent:Notification.Default"
            };
        }

        private void OnToastActivated(ToastNotification sender, object args)
        {
            try
            {
                if (args is ToastActivatedEventArgs activatedArgs)
                {
                    var arguments = activatedArgs.Arguments;
                    
                    if (arguments.Contains("action=show"))
                    {
                        // Show main window
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var mainWindow = Application.Current.MainWindow;
                            if (mainWindow != null)
                            {
                                mainWindow.Show();
                                mainWindow.WindowState = WindowState.Normal;
                                mainWindow.Activate();
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling toast activation: {ex.Message}");
            }
        }

        private void OnToastDismissed(ToastNotification sender, ToastDismissedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine($"Toast dismissed: {args.Reason}");
        }

        private void OnToastFailed(ToastNotification sender, ToastFailedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine($"Toast failed: {args.ErrorCode}");
        }

        public void ShowScheduledNotification(string title, string message, DateTime scheduledTime)
        {
            if (!_notificationsEnabled || _toastNotifier == null)
                return;

            try
            {
                var toastXml = CreateToastXml(title, message, NotificationType.Info);
                var scheduledToast = new ScheduledToastNotification(toastXml, scheduledTime);
                
                _toastNotifier.AddToSchedule(scheduledToast);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to schedule toast notification: {ex.Message}");
            }
        }

        public void ClearAllNotifications()
        {
            try
            {
                _toastNotifier?.Hide(null);
                ToastNotificationManager.History.Clear(APP_ID);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear notifications: {ex.Message}");
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            double number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:N1} {suffixes[counter]}";
        }

        public void Dispose()
        {
            try
            {
                ClearAllNotifications();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing notification service: {ex.Message}");
            }
        }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}