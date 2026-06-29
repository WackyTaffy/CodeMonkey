namespace CodeMonkey.Core.Interfaces
{
    public interface IUserPreferences
    {
        string ProjectRoot { get; set; }
        CodeMonkey.Core.Models.TrustProfile ActiveProfile { get; set; }
        void Save();
        void Load();
    }
}
