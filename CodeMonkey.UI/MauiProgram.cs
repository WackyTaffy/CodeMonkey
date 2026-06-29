using Microsoft.Extensions.DependencyInjection;
using CodeMonkey.UI;

namespace CodeMonkey.UI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.UseBlazorWebView();

            // Configure DI
            DependencyInjection.ConfigureServices(builder.Services);

            return builder.Build();
        }
    }
}
