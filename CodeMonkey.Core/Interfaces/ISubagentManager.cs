using System.Threading.Tasks;
using CodeMonkey.Core.Interfaces;

namespace CodeMonkey.Core.Interfaces
{
    public interface ISubagentManager
    {
        void SetExecutor(IAgentExecutor executor);
        Task<string> HandleSubagentDispatchAsync(string argsYaml, string workingDirectory);
    }
}
