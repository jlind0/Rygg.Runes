
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using ReactiveUI;
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
        public IDispatcherService DispatcherService { get; set; } = null!;
        private readonly Interaction<string, bool> hasPermissions;
        private readonly Interaction<string, bool> alert;
        private readonly Interaction<string, Stream> openFile;
        private readonly Interaction<string, Stream> captureWithCamera;
        private readonly Interaction<string, MemoryStream> saveImage;
        public Interaction<string, MemoryStream> SaveImage
        {
            get => saveImage;
        }
        private byte[]? annoatedImage;
        private bool isLoggedIn = false;
        public bool IsLoggedIn
        {
            get => isLoggedIn;
            set => this.RaiseAndSetIfChanged(ref isLoggedIn, value);
        }
        public byte[]? AnnotatedImage
        {
            get => annoatedImage;
            set => this.RaiseAndSetIfChanged(ref annoatedImage, value);
        }
        private bool isLoading = false;
        public bool IsLoading
        {
            get => isLoading;
            set => this.RaiseAndSetIfChanged(ref isLoading, value);
        }
        private string? question;
        public string? Question
        {
            get => question;
            set => this.RaiseAndSetIfChanged(ref question, value);
        }
        private string? answer;
        public string? Answer
        {
            get => answer;
            set => this.RaiseAndSetIfChanged(ref answer, value);
        }
        public bool HasRunes
        {
            get => this.Runes.Count > 0;
        }
        public bool IsReadyToProcess
        {
            get => CapturedImageBytes != null && !HasRunes;
        }
        public Interaction<string, Stream> CaptureWithCamera => captureWithCamera;
        public Interaction<string, bool> HasPermissions => hasPermissions;
        public Interaction<string, bool> Alert => alert;
        public Interaction<string, Stream> OpenFile => openFile;
        protected IRunesProxy RunesProxy { get; }
        protected IChatGPTProxy ChatProxy { get; }
        public ObservableCollection<string> Runes { get; } = new ObservableCollection<string>();
        public ICommand ProcessImage { get; }
        public ICommand AskFuture { get; }
        public ICommand Login { get; }
        public ICommand TakePhoto { get; }
        public ICommand PickPhoto { get; }
        protected string ApiScope { get; }
        protected IPublicClientApplication ClientApplication { get; }
        protected string SignInSignOutPolicy { get; }
        public MainWindowViewModel(IRunesProxy runesProxy, IChatGPTProxy chatProxy, IConfiguration config, IPublicClientApplication clientApplication) 
        {
            SignInSignOutPolicy = config["AzureAD:SignUpSignInPolicyId"] ?? throw new InvalidDataException();
            ClientApplication = clientApplication;
            RunesProxy = runesProxy;
            hasPermissions = new Interaction<string, bool>();
            alert = new Interaction<string, bool>();
            openFile = new Interaction<string, Stream>();
            ProcessImage = ReactiveCommand.CreateFromTask(DoProcessImage);
            ChatProxy = chatProxy;
            AskFuture = ReactiveCommand.CreateFromTask(DoAskFuture);
            ApiScope = config["MSGraphApi:Scopes"] ?? throw new InvalidDataException();
            Login = ReactiveCommand.CreateFromTask(DoLogin);
            TakePhoto = ReactiveCommand.CreateFromTask(DoTakePhoto);
            captureWithCamera = new Interaction<string, Stream>();
            saveImage = new Interaction<string, MemoryStream>();
            PickPhoto = ReactiveCommand.CreateFromTask(DoPickPhoto);
        }
        private byte[]? capturedImageBytes;
        public byte[]? CapturedImageBytes
        {
            get => capturedImageBytes;
            set => this.RaiseAndSetIfChanged(ref capturedImageBytes, value);
        }
        protected async Task DoTakePhoto(CancellationToken token = default)
        {
            try
            {
                Runes.Clear();
                this.RaisePropertyChanged(nameof(HasRunes));
                if (await HasPermissions.Handle("permissions"))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using var file = await CaptureWithCamera.Handle("Pick an image with Runes").GetAwaiter();
                        if (file != null)
                        {
                            await file.CopyToAsync(memoryStream, token);
                            CapturedImageBytes = memoryStream.ToArray();
                        }
                    }
                    this.RaisePropertyChanged(nameof(IsReadyToProcess));

                }
                else
                    await Alert.Handle("Permissions required").GetAwaiter();
            }
            catch(Exception ex)
            {
                await Alert.Handle(ex.Message).GetAwaiter();
            }
        }
        protected async Task DoPickPhoto(CancellationToken token = default)
        {
            try
            {
                Runes.Clear();
                this.RaisePropertyChanged(nameof(HasRunes));
                using (var memoryStream = new MemoryStream())
                {
                    using var file = await OpenFile.Handle("Pick an image with Runes").GetAwaiter();
                    if (file != null)
                    {
                        await file.CopyToAsync(memoryStream, token);
                        CapturedImageBytes = memoryStream.ToArray();
                    }
                }
                this.RaisePropertyChanged(nameof(IsReadyToProcess));
            }
            catch(Exception ex)
            {
                await Alert.Handle(ex.Message);
            }
        }
        protected async Task ProcessImageRequest(byte[] fileBytes, CancellationToken token = default)
        {
            try
            {
                IsLoading = true;
                if (await HasPermissions.Handle("permissions").GetAwaiter())
                {
                    await DispatcherService.Dispatch(() => Runes.Clear());
                    this.RaisePropertyChanged(nameof(HasRunes));
                    using (SKBitmap sourceBitmap = SKBitmap.Decode(fileBytes))
                    {
                        int sourceWidth = sourceBitmap.Width;
                        int sourceHeight = sourceBitmap.Height;
                        var targetWidth = (sourceWidth > 512 ? 512 : sourceWidth);
                        // Calculate the aspect ratio to maintain the original image's proportions
                        float aspectRatio = (float)sourceWidth / sourceHeight;
                        var targetHeight = (int)(targetWidth / aspectRatio);
                        if(targetHeight > 512)
                        {
                            targetWidth = (int)(aspectRatio * 512);
                            targetHeight = 512;
                        }
                        using (SKBitmap resizedBitmap = sourceBitmap.Resize(new SKImageInfo(targetWidth, targetHeight), SKFilterQuality.Medium))
                        {
                            using (SKImage compressedImage = SKImage.FromBitmap(resizedBitmap))
                            {
                                using (SKData compressedData = compressedImage.Encode(SKEncodedImageFormat.Jpeg, 100))
                                {
                                    fileBytes = compressedData.ToArray();
                                }
                            }
                        }
                    }
                    var resp = await RunesProxy.ProcessImage(fileBytes, token);
                    if (resp != null)
                    {
                        AnnotatedImage = resp.AnnotatedImage;
                        foreach (var rune in resp.Annotations)
                        {
                            Runes.Add(rune);
                        }
                        this.RaisePropertyChanged(nameof(HasRunes));
                        this.RaisePropertyChanged(nameof(IsReadyToProcess));
                    }
                }
                else
                    await Alert.Handle("Permissions required").GetAwaiter();
            }
            catch (Exception ex)
            {
                await Alert.Handle(ex.Message).GetAwaiter();
            }
            finally
            {
                this.IsLoading = false;
            }
        }
        protected async Task DoLogin(CancellationToken token = default)
        {

            bool tryInteractive = false;
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
        protected async Task DoAskFuture(CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(Question))
                return;
            try
            {
                IsLoading = true;
                string answ = await ChatProxy.GetReading(this.Runes.ToArray(), this.Question, token);
                Answer = answ.Replace("\\n", "<br>");
            }
            catch (Exception ex)
            {
                await Alert.Handle(ex.Message).GetAwaiter();
            }
            finally
            {
                IsLoading = false;
            }
        }
        protected async Task DoProcessImage(CancellationToken token = default)
        {
            try
            {
                byte[]? image = null;
                using (var file = await SaveImage.Handle("Get cropped image").GetAwaiter())
                {
                    image = file?.ToArray();
                }
                if(image != null)
                    await ProcessImageRequest(image, token);
            }
            catch(Exception ex)
            {
                await Alert.Handle(ex.Message);
            }
        }
    }
}
