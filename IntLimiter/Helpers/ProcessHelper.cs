using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NetLimiterClone.Helpers
{
    public static class ProcessHelper
    {
        private static readonly ConcurrentDictionary<string, ImageSource> _iconCache = new();

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, string lpIconPath, out ushort lpiIcon);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        public static ImageSource? GetProcessIcon(Process process)
        {
            try
            {
                var processPath = GetProcessPath(process);
                if (string.IsNullOrEmpty(processPath))
                {
                    return GetDefaultIcon();
                }

                if (_iconCache.TryGetValue(processPath, out var cachedIcon))
                {
                    return cachedIcon;
                }

                var icon = ExtractIconFromFile(processPath);
                if (icon != null)
                {
                    _iconCache.TryAdd(processPath, icon);
                }

                return icon ?? GetDefaultIcon();
            }
            catch (Exception)
            {
                return GetDefaultIcon();
            }
        }

        public static string GetProcessPath(Process process)
        {
            try
            {
                return process.MainModule?.FileName ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static ProcessDetails GetProcessDetails(Process process)
        {
            try
            {
                var details = new ProcessDetails
                {
                    ProcessId = process.Id,
                    ProcessName = process.ProcessName,
                    ProcessPath = GetProcessPath(process),
                    StartTime = process.StartTime,
                    WorkingSet = process.WorkingSet64,
                    PagedMemorySize = process.PagedMemorySize64,
                    VirtualMemorySize = process.VirtualMemorySize64
                };

                // Get file version info
                if (!string.IsNullOrEmpty(details.ProcessPath) && File.Exists(details.ProcessPath))
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(details.ProcessPath);
                    details.CompanyName = versionInfo.CompanyName ?? string.Empty;
                    details.ProductName = versionInfo.ProductName ?? string.Empty;
                    details.FileDescription = versionInfo.FileDescription ?? string.Empty;
                    details.FileVersion = versionInfo.FileVersion ?? string.Empty;
                    details.ProductVersion = versionInfo.ProductVersion ?? string.Empty;
                }

                return details;
            }
            catch (Exception)
            {
                return new ProcessDetails
                {
                    ProcessId = process.Id,
                    ProcessName = process.ProcessName
                };
            }
        }

        private static ImageSource? ExtractIconFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                var hInst = GetModuleHandle(null);
                var hIcon = ExtractAssociatedIcon(hInst, filePath, out _);

                if (hIcon == IntPtr.Zero)
                    return null;

                try
                {
                    var icon = Icon.FromHandle(hIcon);
                    var bitmap = icon.ToBitmap();
                    
                    var bitmapSource = ConvertBitmapToBitmapSource(bitmap);
                    bitmapSource.Freeze();
                    
                    bitmap.Dispose();
                    icon.Dispose();
                    
                    return bitmapSource;
                }
                finally
                {
                    DestroyIcon(hIcon);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            var hBitmap = bitmap.GetHbitmap();
            try
            {
                var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                
                return bitmapSource;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private static ImageSource GetDefaultIcon()
        {
            try
            {
                // Create a simple default icon
                var drawingVisual = new DrawingVisual();
                using (var context = drawingVisual.RenderOpen())
                {
                    context.DrawRectangle(System.Windows.Media.Brushes.Gray, new System.Windows.Media.Pen(System.Windows.Media.Brushes.DarkGray, 1), new Rect(0, 0, 16, 16));
                    context.DrawText(
                        new FormattedText("?", 
                            System.Globalization.CultureInfo.CurrentCulture, 
                            FlowDirection.LeftToRight, 
                            new Typeface("Arial"), 
                            10, 
                            System.Windows.Media.Brushes.White, 
                            VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip),
                        new System.Windows.Point(5, 2));
                }

                var renderTarget = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
                renderTarget.Render(drawingVisual);
                renderTarget.Freeze();
                
                return renderTarget;
            }
            catch (Exception)
            {
                return null!;
            }
        }

        public static bool IsSystemProcess(Process process)
        {
            try
            {
                var systemProcesses = new[]
                {
                    "System", "Idle", "csrss", "winlogon", "services", "lsass", "svchost",
                    "explorer", "dwm", "winlogon", "smss", "wininit", "ntoskrnl", "Registry"
                };

                return Array.Exists(systemProcesses, name => 
                    string.Equals(name, process.ProcessName, StringComparison.OrdinalIgnoreCase)) ||
                       process.SessionId == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void ClearIconCache()
        {
            _iconCache.Clear();
        }
    }

    public class ProcessDetails
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string ProcessPath { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public long WorkingSet { get; set; }
        public long PagedMemorySize { get; set; }
        public long VirtualMemorySize { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string FileDescription { get; set; } = string.Empty;
        public string FileVersion { get; set; } = string.Empty;
        public string ProductVersion { get; set; } = string.Empty;

        public string WorkingSetFormatted => FormatBytes(WorkingSet);
        public string PagedMemorySizeFormatted => FormatBytes(PagedMemorySize);
        public string VirtualMemorySizeFormatted => FormatBytes(VirtualMemorySize);

        private string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024.0 * 1024):F1} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F1} KB";
            return $"{bytes} B";
        }
    }
}