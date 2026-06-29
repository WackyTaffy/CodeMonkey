namespace CodeMonkey.Core.Interfaces
{
    public interface IUserPreferences
    {
        string ProjectRoot { get; set; }
        TrustProfile ActiveProfile { get; set; }
        void Save();
        void Load();
    }

    public enum TrustProfile
    {
        Strict,
        Balanced,
        Trusting
    }
}
