using CodeMonkey.Core.Interfaces;

namespace CodeMonkey.Core.Services
{
    public class SessionLedger : ISessionLedger
    {
        private readonly List<(string, bool, string, DateTime)> _history = new();
        private readonly string _ledgerPath;

        public SessionLedger()
        {
            _ledgerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "session.ledger");
        }

        public void RecordAction(string action, bool success, string details)
        {
            _history.Add((action, success, details, DateTime.Now));
            
            try
            {
                var entry = $"{DateTime.Now:O}|{action}|{success}|{details}{Environment.NewLine}";
                File.AppendAllText(_ledgerPath, entry);
            }
            catch { /* Silent fail for ledger logging */ }
        }

        public IEnumerable<(string Action, bool Success, string Details, DateTime Timestamp)> GetHistory()
        {
            return _history;
        }

        public void Clear()
        {
            _history.Clear();
            try
            {
                if (File.Exists(_ledgerPath)) File.Delete(_ledgerPath);
            }
            catch { }
        }
    }
}
