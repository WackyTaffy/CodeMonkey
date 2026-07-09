using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using CodeMonkey.Core.Services;

namespace CodeMonkey.UI.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        private readonly IOrchestrator _orchestrator;
        private readonly ILogManager _logManager;
        private string _userInput = string.Empty;
        private bool _isProcessing = false;
        private string _currentProjectRoot = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public string UserInput
        {
            get => _userInput;
            set
            {
                _userInput = value;
                OnPropertyChanged();
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged();
            }
        }

        public string CurrentProjectRoot
        {
            get => _currentProjectRoot;
            set
            {
                _currentProjectRoot = value;
                OnPropertyChanged();
            }
        }

        public ChatViewModel(IOrchestrator orchestrator, ILogManager logManager)
        {
            _orchestrator = orchestrator;
            _logManager = logManager;
            _orchestrator.OnStatusUpdate = HandleStatusUpdate;
            
            // Initialize project root to current directory by default
            _currentProjectRoot = System.IO.Directory.GetCurrentDirectory();
            
            // Bootstrap the orchestrator context
            _orchestrator.BootstrapContext(_currentProjectRoot);
            _logManager.Log($"ChatViewModel initialized. Project root: {_currentProjectRoot}");
        }

        public async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(UserInput) || IsProcessing) return;

            var userMsg = UserInput;
            Messages.Add(new ChatMessage { Role = "user", Content = userMsg });
            
            var assistantMsg = new ChatMessage { Role = "assistant", Content = "Thinking..." };
            Messages.Add(assistantMsg);

            UserInput = string.Empty;
            IsProcessing = true;

            try
            {
                _logManager.Log($"User request: {userMsg}");
                ToolResult toolResult = await _orchestrator.ProcessUserRequestAsync(userMsg, CurrentProjectRoot);
                
                // Update the message in the collection to trigger UI refresh
                int index = Messages.IndexOf(assistantMsg);
                if (index != -1)
                {
                    Messages[index] = new ChatMessage { Role = "assistant", Content = toolResult.Result };
                }
            }
            catch (Exception ex)
            {
                _logManager.Log($"Error processing request: {ex.Message}");
                int index = Messages.IndexOf(assistantMsg);
                if (index != -1)
                {
                    Messages[index] = new ChatMessage { Role = "assistant", Content = $"Error: {ex.Message}" };
                }
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void HandleStatusUpdate(string status)
        {
            _logManager.Log(status);
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
