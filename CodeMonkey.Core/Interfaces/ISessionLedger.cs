namespace CodeMonkey.Core.Interfaces
{
    public interface ISessionLedger
    {
        void RecordAction(string action, bool success, string details);
        IEnumerable<(string Action, bool Success, string Details, DateTime Timestamp)> GetHistory();
        void Clear();
    }
}
