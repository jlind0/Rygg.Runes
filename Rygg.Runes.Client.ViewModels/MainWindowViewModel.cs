
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using ReactiveUI;
using Rygg.Runes.Data.Embedded;
using RyggRunes.Client.Core;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reactive.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Formats.Asn1.AsnWriter;

namespace Rygg.Runes.Client.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly Interaction<string, bool> hasPermissions;
        private readonly Interaction<string, bool> alert;
        private readonly Interaction<string, Stream> openFile;
        private readonly Interaction<string, Stream> captureWithCamera;
        private readonly Interaction<string, MemoryStream> saveImage;
        private double screenWidth;
        public double ScreenWidth
        {
            get => screenWidth;
            set => this.RaiseAndSetIfChanged(ref screenWidth, value);
        }
        private double screenHeight;
        public double ScreenHeight
        {
            get => screenHeight;
            set => this.RaiseAndSetIfChanged(ref screenHeight, value);
        }
        public Interaction<string, MemoryStream> SaveImage
        {
            get => saveImage;
        }
        
        private bool isLoggedIn = false;
        public bool IsLoggedIn
        {
            get => isLoggedIn;
            set => this.RaiseAndSetIfChanged(ref isLoggedIn, value);
        }
        
        private bool isLoading = false;
        public bool IsLoading
        {
            get => isLoading;
            set => this.RaiseAndSetIfChanged(ref isLoading, value);
        }
        public Interaction<string, Stream> CaptureWithCamera => captureWithCamera;
        public Interaction<string, bool> HasPermissions => hasPermissions;
        public Interaction<string, bool> Alert => alert;
        public Interaction<string, Stream> OpenFile => openFile;
        protected IRunesProxy RunesProxy { get; }
        protected IChatGPTProxy ChatProxy { get; }
        
        public ICommand Login { get; }
        
        protected string ApiScope { get; }
        protected IPublicClientApplication ClientApplication { get; }
        protected string SignInSignOutPolicy { get; }
        protected IReadingsDataAdapter ReadingsDataAdapter { get; }
        public DetectorViewModel DetectorVM { get; }
        public ReadingsViewModel ReadingsVM { get; }
        public MainWindowViewModel(IRunesProxy runesProxy, IChatGPTProxy chatProxy, 
            IConfiguration config, IPublicClientApplication clientApplication, 
            IReadingsDataAdapter readingsDataAdapter) 
        {
            DetectorVM = new DetectorViewModel(this, runesProxy, chatProxy, config, readingsDataAdapter);
            ReadingsVM = new ReadingsViewModel(this, readingsDataAdapter);
            SignInSignOutPolicy = config["AzureAD:SignUpSignInPolicyId"] ?? throw new InvalidDataException();
            ClientApplication = clientApplication;
            RunesProxy = runesProxy;
            hasPermissions = new Interaction<string, bool>();
            alert = new Interaction<string, bool>();
            openFile = new Interaction<string, Stream>();
            
            ChatProxy = chatProxy;
            
            ApiScope = config["MSGraphApi:Scopes"] ?? throw new InvalidDataException();
            Login = ReactiveCommand.CreateFromTask<bool>(DoLogin);
            
            captureWithCamera = new Interaction<string, Stream>();
            saveImage = new Interaction<string, MemoryStream>();
            ReadingsDataAdapter = readingsDataAdapter;
        }
       
       
        public async Task DoLogin(bool forceInteractive = false, CancellationToken token = default)
        {

            bool tryInteractive = false;
            if (!forceInteractive)
            {
                try
                {
                    var accounts = (await ClientApplication.GetAccountsAsync(SignInSignOutPolicy)).ToList();
                    if (accounts.Any())
                    {
                        var result = await ClientApplication.AcquireTokenSilent(new string[] { ApiScope }, accounts.First()).ExecuteAsync();
                        IsLoggedIn = !string.IsNullOrEmpty(result.AccessToken);
                        tryInteractive = !IsLoggedIn;
                    }
                    else
                        tryInteractive = true;
                }
                catch (MsalUiRequiredException)
                {
                    tryInteractive = true;
                }
                catch (Exception ex)
                {
                    await Alert.Handle(ex.Message).GetAwaiter();
                }
            }
            else
                tryInteractive = true;
            if (tryInteractive)
            {
                try
                {
                    var result = await ClientApplication.AcquireTokenInteractive(new string[] { ApiScope })
                                .WithPrompt(Prompt.SelectAccount).ExecuteAsync(token);
                    IsLoggedIn = !string.IsNullOrEmpty(result.AccessToken);

                }
                catch (Exception ex)
                {
                    await Alert.Handle(ex.Message).GetAwaiter();
                }
            }
        }
        
    }
}
