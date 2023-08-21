using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RyggRunes.Client.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rygg.Runes.Client.ViewModels;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Abstractions;
using Rygg.Runes.Proxy;
using System.Reflection;

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

            var a = Assembly.GetExecutingAssembly();
            using (var stream = a.GetManifestResourceStream("RyggRunes.MAUI.Client.appsettings.json"))
            {

                builder.Configuration.AddJsonStream(stream);
            }
            builder.Services.AddSingleton(provider =>
            {
                return PublicClientApplicationBuilder.Create(builder.Configuration["AzureAD:ClientId"])
                .WithB2CAuthority(builder.Configuration["AzureAD:Authority"])
#if WINDOWS
                .WithRedirectUri(builder.Configuration["AzureAD:RedirectURI"]) // needed only for the system browser
#elif IOS
                .WithRedirectUri(builder.Configuration["AzureAD:iOSRedirectURI"])
#elif MACCATALYST
                .WithRedirectUri(builder.Configuration["AzureAD:iOSRedirectURI"])
#endif                
                .Build();

            });
            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
            builder.Services.AddSingleton<IRunesProxy, RunesProxy>();
            builder.Services.AddSingleton<IChatGPTProxy, MysticProxy>();
            builder.Services.AddScoped<MainWindowViewModel>();
            builder.Services.TryAddTransient<MainPage>();
            return builder.Build();
        }
    }
}