using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RyggRunes.Client.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rygg.Runes.Client.ViewModels;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Abstractions;
using Rygg.Runes.Proxy;
using System.Reflection;
using Telerik.Maui.Controls.Compatibility;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Markup;
using Rygg.Runes.Data.Embedded;

namespace RyggRunes.MAUI.Client
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseTelerik().UseMauiCommunityToolkit().UseMauiCommunityToolkitMarkup()
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("FA-Solid-900.otf", "FASolid");
                });

            var a = Assembly.GetExecutingAssembly();
            using (var stream = a.GetManifestResourceStream("RyggRunes.MAUI.Client.appsettings.json"))
            {

                builder.Configuration.AddJsonStream(stream);
            }
            builder.Services.AddSingleton(provider =>
            {
                var client = PublicClientApplicationBuilder.Create(builder.Configuration["AzureAD:ClientId"])
                .WithB2CAuthority(builder.Configuration["AzureAD:Authority"])
#if WINDOWS
                .WithRedirectUri(builder.Configuration["AzureAD:RedirectURI"]) // needed only for the system browser
#elif IOS
                .WithRedirectUri(builder.Configuration["AzureAD:iOSRedirectURI"])
                .WithIosKeychainSecurityGroup(builder.Configuration["AzureAD:iOSKeyChainGroup"])
#elif MACCATALYST
                .WithRedirectUri(builder.Configuration["AzureAD:iOSRedirectURI"])
#endif                
                .Build();
#if WINDOWS || MACCATALYST
                string fileName = Path.Join(FileSystem.CacheDirectory, "msal.token.cache2");
                client.UserTokenCache.SetBeforeAccessAsync(async args =>
                {
                    if (!(await FileSystem.Current.AppPackageFileExistsAsync(fileName)))
                        return;
                    byte[] fileBytes;
                    using (var stream = new FileStream(fileName, FileMode.Open))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await stream.CopyToAsync(memoryStream);
                            fileBytes = memoryStream.ToArray();
                        }
                    }
                    args.TokenCache.DeserializeMsalV3(fileBytes);
                });
                client.UserTokenCache.SetAfterAccessAsync(async args =>
                {
                    if (args.HasStateChanged)
                    {
                        var data = args.TokenCache.SerializeMsalV3();
                        using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                        {
                            await fs.WriteAsync(data, 0, data.Length);
                        }
                    }
                });
#endif
                return client;
            });
            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
            builder.Services.AddSingleton<IRunesProxy, RunesProxy>();
            builder.Services.AddSingleton<IChatGPTProxy, MysticProxy>();
            builder.Services.AddSingleton<IReadingsDataAdapter>(new ReadingsDataAdapter(FileSystem.AppDataDirectory));
            builder.Services.AddScoped<MainWindowViewModel>();
            builder.Services.TryAddTransient<MainPage>();
            return builder.Build();
        }
    }
}