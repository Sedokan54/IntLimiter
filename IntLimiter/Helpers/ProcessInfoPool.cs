using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NetLimiterClone.Models;

namespace NetLimiterClone.Helpers
{
    public class ProcessInfoPool
    {
        private readonly ConcurrentQueue<ProcessInfo> _pool = new();
        private readonly int _maxPoolSize;
        private int _currentPoolSize;

        public ProcessInfoPool(int maxPoolSize = 100)
        {
            _maxPoolSize = maxPoolSize;
        }

        public ProcessInfo GetOrCreate()
        {
            if (_pool.TryDequeue(out ProcessInfo? pooledItem))
            {
                _currentPoolSize--;
                ResetProcessInfo(pooledItem);
                return pooledItem;
            }

            return new ProcessInfo();
        }

        public void Return(ProcessInfo processInfo)
        {
            if (_currentPoolSize < _maxPoolSize)
            {
                _pool.Enqueue(processInfo);
                _currentPoolSize++;
            }
        }

        private void ResetProcessInfo(ProcessInfo processInfo)
        {
            processInfo.ProcessId = 0;
            processInfo.ProcessName = string.Empty;
            processInfo.ProcessPath = string.Empty;
            processInfo.Icon = null;
            processInfo.DownloadSpeed = 0;
            processInfo.UploadSpeed = 0;
            processInfo.TotalDownload = 0;
            processInfo.TotalUpload = 0;
            processInfo.ConnectionCount = 0;
            processInfo.IsSystemProcess = false;
            processInfo.DownloadLimit = 0;
            processInfo.UploadLimit = 0;
            processInfo.IsLimited = false;
            processInfo.IconLoaded = false;
            processInfo.DetailsLoaded = false;
        }

        public int CurrentPoolSize => _currentPoolSize;
        public int MaxPoolSize => _maxPoolSize;
    }
}