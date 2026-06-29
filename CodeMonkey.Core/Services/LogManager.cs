using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CodeMonkey.Core.Services
{
    public interface ILogManager
    {
        void Log(string message);
        IEnumerable<string> GetRecentLogs(int count);
        event Action<string>? OnLogAdded;
    }

    public class LogManager : ILogManager
    {
        private readonly ConcurrentQueue<string> _buffer = new();
        private readonly string _logFilePath;
        private const int MaxBufferSize = 1000;

        public event Action<string>? OnLogAdded;

        public LogManager()
        {
            _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "codemonkey.log");
        }

        public void Log(string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            
            _buffer.Enqueue(logEntry);
            while (_buffer.Count > MaxBufferSize)
            {
                _buffer.TryDequeue(out _);
            }

            try
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch { /* Silently ignore log file errors to prevent UI crash */ }

            OnLogAdded?.Invoke(logEntry);
        }

        public IEnumerable<string> GetRecentLogs(int count)
        {
            var list = new List<string>(_buffer);
            int start = Math.Max(0, list.Count - count);
            return list.GetRange(start, list.Count - start);
        }
    }
}
