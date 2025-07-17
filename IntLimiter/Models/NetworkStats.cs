using System;

namespace NetLimiterClone.Models
{
    public class NetworkStats
    {
        public DateTime Timestamp { get; set; }
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;

        public long TotalBytes => BytesSent + BytesReceived;

        public NetworkStats()
        {
            Timestamp = DateTime.Now;
        }

        public NetworkStats(int processId, string processName, long bytesSent, long bytesReceived)
        {
            ProcessId = processId;
            ProcessName = processName;
            BytesSent = bytesSent;
            BytesReceived = bytesReceived;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] {ProcessName} (PID: {ProcessId}) - Sent: {BytesSent:N0} bytes, Received: {BytesReceived:N0} bytes";
        }
    }
}