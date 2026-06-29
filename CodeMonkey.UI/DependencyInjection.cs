using Microsoft.Extensions.DependencyInjection;
using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Services;
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
            
            // General Services
            services.AddSingleton<IUserPreferences, UserPreferences>();
            services.AddSingleton<ISessionLedger, SessionLedger>();
            services.AddSingleton<IGitService, GitService>();
            services.AddSingleton<ILogManager, LogManager>();
        }
    }
}
