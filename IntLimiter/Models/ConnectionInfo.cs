using System;
using System.Net;

namespace NetLimiterClone.Models
{
    public class ConnectionInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
        public IPAddress LocalAddress { get; set; } = IPAddress.Any;
        public int LocalPort { get; set; }
        public IPAddress RemoteAddress { get; set; } = IPAddress.Any;
        public int RemotePort { get; set; }
        public ConnectionState State { get; set; } = ConnectionState.Unknown;
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }

        // Formatted properties for display
        public string LocalAddressString => LocalAddress.ToString();
        public string RemoteAddressString => RemoteAddress.ToString();
        public string StateString => State.ToString();
        public string BytesSentFormatted => FormatBytes(BytesSent);
        public string BytesReceivedFormatted => FormatBytes(BytesReceived);
        public string DurationFormatted => (DateTime.Now - CreatedTime).ToString(@"hh\:mm\:ss");
        public string LocalEndpoint => $"{LocalAddress}:{LocalPort}";
        public string RemoteEndpoint => $"{RemoteAddress}:{RemotePort}";

        private static string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 B";
            
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
    }

    public enum ConnectionState
    {
        Unknown,
        Closed,
        Listen,
        SynSent,
        SynRcvd,
        Established,
        FinWait1,
        FinWait2,
        CloseWait,
        Closing,
        LastAck,
        TimeWait,
        DeleteTcb
    }
}