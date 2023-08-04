using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RyggRunes.Client.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rygg.Runes.Client.ViewModels;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;

namespace RyggRunes.MAUI.Client
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>().UseMauiCommunityToolkit().UseMauiCommunityToolkitMarkup()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Configuration.AddJsonFile("appsettings.json");

            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
            builder.Services.AddSingleton<IRunesProxy, RunesProxy>();
            builder.Services.AddSingleton<IChatGPTProxy, ChatGPTProxy>();
            builder.Services.AddScoped<MainWindowViewModel>();
            builder.Services.TryAddTransient<MainPage>();
            return builder.Build();
        }
    }
}