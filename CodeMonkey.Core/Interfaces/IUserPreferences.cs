namespace CodeMonkey.Core.Interfaces
{
    public interface IUserPreferences
    {
        string ProjectRoot { get; set; }
        void Save();
        void Load();
    }
}
