using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using NetLimiterClone.Models;

namespace NetLimiterClone.Services
{
    public class SystemTrayService : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _contextMenu;
        private bool _disposed = false;

        public event EventHandler? ShowMainWindowRequested;
        public event EventHandler? HideMainWindowRequested;
        public event EventHandler? ExitApplicationRequested;

        public SystemTrayService()
        {
            _notifyIcon = new NotifyIcon();
            _contextMenu = CreateContextMenu();

            InitializeNotifyIcon();
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon.Icon = CreateDefaultIcon();
            _notifyIcon.Text = "NetLimiter Clone";
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenuStrip = _contextMenu;

            _notifyIcon.DoubleClick += (sender, e) => ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);
            _notifyIcon.MouseClick += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);
                }
            };
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();

            var showItem = new ToolStripMenuItem("Show Main Window");
            showItem.Click += (sender, e) => ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);
            showItem.Font = new Font(showItem.Font, FontStyle.Bold);

            var hideItem = new ToolStripMenuItem("Hide to Tray");
            hideItem.Click += (sender, e) => HideMainWindowRequested?.Invoke(this, EventArgs.Empty);

            var separatorItem = new ToolStripSeparator();

            var statsItem = new ToolStripMenuItem("Quick Stats");
            var downloadStatsItem = new ToolStripMenuItem("Download: 0 KB/s");
            var uploadStatsItem = new ToolStripMenuItem("Upload: 0 KB/s");
            var activeProcessesItem = new ToolStripMenuItem("Active Processes: 0");

            downloadStatsItem.Enabled = false;
            uploadStatsItem.Enabled = false;
            activeProcessesItem.Enabled = false;

            statsItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                downloadStatsItem,
                uploadStatsItem,
                activeProcessesItem
            });

            var separator2Item = new ToolStripSeparator();

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (sender, e) => ExitApplicationRequested?.Invoke(this, EventArgs.Empty);

            menu.Items.AddRange(new ToolStripItem[]
            {
                showItem,
                hideItem,
                separatorItem,
                statsItem,
                separator2Item,
                exitItem
            });

            return menu;
        }

        private Icon CreateDefaultIcon()
        {
            try
            {
                // Create a simple icon using System.Drawing
                using var bitmap = new Bitmap(16, 16);
                using var graphics = Graphics.FromImage(bitmap);
                
                graphics.Clear(Color.Transparent);
                graphics.FillEllipse(Brushes.Blue, 2, 2, 12, 12);
                graphics.DrawString("N", new Font("Arial", 8, FontStyle.Bold), Brushes.White, new PointF(5, 2));

                var hIcon = bitmap.GetHicon();
                return Icon.FromHandle(hIcon);
            }
            catch (Exception)
            {
                // Fallback to system icon
                return SystemIcons.Application;
            }
        }

        public void UpdateTooltip(string downloadSpeed, string uploadSpeed, int activeProcesses, int limitedProcesses)
        {
            var tooltip = $"NetLimiter Clone\n" +
                         $"Download: {downloadSpeed}\n" +
                         $"Upload: {uploadSpeed}\n" +
                         $"Active: {activeProcesses} | Limited: {limitedProcesses}";

            if (tooltip.Length > 63) // NotifyIcon tooltip limit
            {
                tooltip = tooltip.Substring(0, 60) + "...";
            }

            _notifyIcon.Text = tooltip;

            // Update context menu stats
            if (_contextMenu.Items.Count > 3 && _contextMenu.Items[3] is ToolStripMenuItem statsItem)
            {
                if (statsItem.DropDownItems.Count >= 3)
                {
                    ((ToolStripMenuItem)statsItem.DropDownItems[0]).Text = $"Download: {downloadSpeed}";
                    ((ToolStripMenuItem)statsItem.DropDownItems[1]).Text = $"Upload: {uploadSpeed}";
                    ((ToolStripMenuItem)statsItem.DropDownItems[2]).Text = $"Active Processes: {activeProcesses}";
                }
            }
        }

        public void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info, int timeout = 3000)
        {
            _notifyIcon.ShowBalloonTip(timeout, title, text, icon);
        }

        public void ShowNotification(string message, NotificationType type = NotificationType.Info)
        {
            var icon = type switch
            {
                NotificationType.Warning => ToolTipIcon.Warning,
                NotificationType.Error => ToolTipIcon.Error,
                NotificationType.Success => ToolTipIcon.Info,
                _ => ToolTipIcon.Info
            };

            ShowBalloonTip("NetLimiter Clone", message, icon);
        }

        public void SetIcon(Icon icon)
        {
            _notifyIcon.Icon?.Dispose();
            _notifyIcon.Icon = icon;
        }

        public void UpdateIconForActivity(bool hasActivity)
        {
            try
            {
                using var bitmap = new Bitmap(16, 16);
                using var graphics = Graphics.FromImage(bitmap);
                
                graphics.Clear(Color.Transparent);
                
                var color = hasActivity ? Color.Green : Color.Blue;
                graphics.FillEllipse(new SolidBrush(color), 2, 2, 12, 12);
                graphics.DrawString("N", new Font("Arial", 8, FontStyle.Bold), Brushes.White, new PointF(5, 2));

                var oldIcon = _notifyIcon.Icon;
                var hIcon = bitmap.GetHicon();
                _notifyIcon.Icon = Icon.FromHandle(hIcon);
                oldIcon?.Dispose();
            }
            catch (Exception)
            {
                // Ignore icon update errors
            }
        }

        public void Hide()
        {
            _notifyIcon.Visible = false;
        }

        public void Show()
        {
            _notifyIcon.Visible = true;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _notifyIcon?.Dispose();
                _contextMenu?.Dispose();
                _disposed = true;
            }
        }
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success
    }
}