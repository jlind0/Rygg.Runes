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
using MAUI.MSALClient;
using Rygg.Runes.Proxy;
using Microsoft.Identity.Client;

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
            
            //PlatformConfig.Instance.RedirectUri = PublicClientSingleton.Instance.MSALClientHelper.AzureADConfig.RedirectURI;
            builder.Configuration.AddJsonFile("appsettings.json");
            
            builder.Services.AddSingleton(provider =>
            {
                return PublicClientApplicationBuilder.Create(builder.Configuration["AzureAD:ClientId"])
                .WithB2CAuthority(builder.Configuration["AzureAD:Authority"])
                .WithRedirectUri(builder.Configuration["AzureAD:RedirectURI"]) // needed only for the system browser
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