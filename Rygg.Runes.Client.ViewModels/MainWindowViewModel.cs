﻿
using MAUI.MSALClient;
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
        private readonly Interaction<string, byte[]> captureWithCamera;
        private byte[] annoatedImage;
        private bool isLoggedIn = false;
        public bool IsLoggedIn
        {
            get => isLoggedIn;
            set => this.RaiseAndSetIfChanged(ref isLoggedIn, value);
        }
        public byte[] AnnotatedImage
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
        private string question;
        public string Question
        {
            get => question;
            set => this.RaiseAndSetIfChanged(ref question, value);
        }
        private string answer;
        public string Answer
        {
            get => answer;
            set => this.RaiseAndSetIfChanged(ref answer, value);
        }
        public bool HasRunes
        {
            get => this.Runes.Count > 0;
        }
        public Interaction<string, byte[]> CaptureWithCamera => captureWithCamera;
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
        protected string ApiScope { get; }
        protected IPublicClientApplication ClientApplication { get; }
        public MainWindowViewModel(IRunesProxy runesProxy, IChatGPTProxy chatProxy, IConfiguration config, IPublicClientApplication clientApplication) 
        {
            ClientApplication = clientApplication;
            RunesProxy = runesProxy;
            hasPermissions = new Interaction<string, bool>();
            alert = new Interaction<string, bool>();
            openFile = new Interaction<string, Stream>();
            ProcessImage = ReactiveCommand.CreateFromTask(DoProcessImage);
            ChatProxy = chatProxy;
            AskFuture = ReactiveCommand.CreateFromTask(DoAskFuture);
            ApiScope = config["MSGraphApi:Scopes"];
            Login = ReactiveCommand.CreateFromTask(DoLogin);
            TakePhoto = ReactiveCommand.CreateFromTask(DoTakePhoto);
            captureWithCamera = new Interaction<string, byte[]>();
        }
        protected async Task DoTakePhoto(CancellationToken token = default)
        {
            if(await HasPermissions.Handle("permissions"))
            {
                var fileBytes = await CaptureWithCamera.Handle("image").GetAwaiter();
                if(fileBytes != null)
                    await ProcessImageRequest(fileBytes, token);
            }
            else
                await Alert.Handle("Permissions required").GetAwaiter();
        }
        protected async Task ProcessImageRequest(byte[] fileBytes, CancellationToken token = default)
        {
            this.IsLoading = true;
            if (await HasPermissions.Handle("permissions").GetAwaiter())
            {
                Runes.Clear();
                this.RaisePropertyChanged(nameof(HasRunes));
                using (SKBitmap sourceBitmap = SKBitmap.Decode(fileBytes))
                {
                    int sourceWidth = sourceBitmap.Width;
                    int sourceHeight = sourceBitmap.Height;
                    var targetWidth = (sourceWidth > 1000 ? 1000 : sourceWidth);
                    // Calculate the aspect ratio to maintain the original image's proportions
                    float aspectRatio = (float)sourceWidth / sourceHeight;
                    var targetHeight = (int)(targetWidth / aspectRatio);

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
                }
            }
            else
                await Alert.Handle("Permissions required").GetAwaiter();
            this.IsLoading = false;
        }
        protected async Task DoLogin(CancellationToken token = default)
        {
            
            try
            {
               var result = await ClientApplication.AcquireTokenInteractive(new string[] {ApiScope})
                        .WithPrompt(Prompt.SelectAccount).ExecuteAsync(token)
                        .ConfigureAwait(false);
                IsLoggedIn = true;
            }
            catch (Exception ex)
            {
                throw;
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
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }
        protected async Task DoProcessImage(CancellationToken token = default)
        {
            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                using var file = await OpenFile.Handle("Pick an image with Runes").GetAwaiter();
                await file.CopyToAsync(memoryStream, token);
                fileBytes = memoryStream.ToArray();
            }
            await ProcessImageRequest(fileBytes, token);
        }
    }
}
