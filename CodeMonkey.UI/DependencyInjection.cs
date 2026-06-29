using Microsoft.Extensions.DependencyInjection;
using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Services;
using CodeMonkey.UI.Rendering.Services;
using CodeMonkey.UI.ViewModels;
using System.IO;
using System.Collections.Generic;
using System;

namespace CodeMonkey.UI
{
    public static class DependencyInjection
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            // Core Services
            services.AddSingleton<IOrchestrator, Orchestrator>();
            services.AddSingleton<IProcessRunner, ProcessRunner>();
            
            // General Services
            services.AddSingleton<IUserPreferences, UserPreferences>();
            services.AddSingleton<ISessionLedger, SessionLedger>();
            services.AddSingleton<IGitService, GitService>();
            services.AddSingleton<ILogManager, LogManager>();

            // Rendering Services
            services.AddSingleton<IMarkdownComponentRenderer, MarkdownComponentRenderer>();

            // ViewModels
            services.AddSingleton<ChatViewModel>();
        }
    }
}
