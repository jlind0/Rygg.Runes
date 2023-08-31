using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using ReactiveUI;
using Rygg.Runes.Data.Embedded;
using RyggRunes.Client.Core;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Rygg.Runes.Client.ViewModels
{
    public class DetectorViewModel : ReactiveObject
    {
        private bool hasSaved;
        public bool HasSaved
        {
            get => hasSaved;
            set => this.RaiseAndSetIfChanged(ref hasSaved, value);
        }
        private byte[]? capturedImageBytes;
        public byte[]? CapturedImageBytes
        {
            get => capturedImageBytes;
            set => this.RaiseAndSetIfChanged(ref capturedImageBytes, value);
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
        public bool CanSave
        {
            get => HasRunes && AnnotatedImage != null && 
                !string.IsNullOrWhiteSpace(Answer) && 
                !string.IsNullOrEmpty(Question) && !HasSaved;
        }
        public ICommand TakePhoto { get; }
        public ICommand PickPhoto { get; }

        public ICommand AskFuture { get; }
        public ObservableCollection<string> Runes { get; } = new ObservableCollection<string>();
        public MainWindowViewModel Parent { get; }
        protected IRunesProxy RunesProxy { get; }
        protected IChatGPTProxy ChatProxy { get; }
        protected IConfiguration Config { get; }
        protected IReadingsDataAdapter ReadingsAdapter { get; }
        public ICommand SaveReading { get; }
        public ICommand ProcessImage { get; }
        private byte[]? annoatedImage;
        public byte[]? AnnotatedImage
        {
            get => annoatedImage;
            set => this.RaiseAndSetIfChanged(ref annoatedImage, value);
        }

        public DetectorViewModel(MainWindowViewModel parent, IRunesProxy runesProxy, IChatGPTProxy chatProxy,
            IConfiguration config,
            IReadingsDataAdapter readingsAdapter)
        {
            Parent = parent;
            RunesProxy = runesProxy;
            ChatProxy = chatProxy;
            Config = config;
            ReadingsAdapter = readingsAdapter;
            ProcessImage = ReactiveCommand.CreateFromTask(DoProcessImage);
            TakePhoto = ReactiveCommand.CreateFromTask(DoTakePhoto);
            PickPhoto = ReactiveCommand.CreateFromTask(DoPickPhoto);
            AskFuture = ReactiveCommand.CreateFromTask(DoAskFuture);
            SaveReading = ReactiveCommand.CreateFromTask(DoSaveReading);
        }
        protected async Task DoSaveReading(CancellationToken token = default)
        {
            try
            {
                if (CanSave)
                {
                    Parent.IsLoading = true;
                    await ReadingsAdapter.Add(new Reading()
                    {
                        AnnotatedImage = AnnotatedImage ?? throw new InvalidDataException(),
                        Runes = Runes.ToArray(),
                        Question = Question ?? throw new InvalidDataException(),
                        Answer = Answer ?? throw new InvalidDataException()

                    }, token);
                    this.HasSaved = true;
                    this.RaisePropertyChanged(nameof(CanSave));
                }
            }
            catch(Exception ex)
            {
                await Parent.Alert.Handle(ex.Message).GetAwaiter();
            }
            Parent.IsLoading = false;
        }
        protected void Reset()
        {
            Runes.Clear();
            Answer = null;
            Question = null;
            HasSaved = false;
            this.RaisePropertyChanged(nameof(HasRunes));
            this.RaisePropertyChanged(nameof(CanSave));
        }
        protected async Task DoTakePhoto(CancellationToken token = default)
        {
            try
            {
                Reset();
                
                if (await Parent.HasPermissions.Handle("permissions"))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using var file = await Parent.CaptureWithCamera.Handle("Pick an image with Runes").GetAwaiter();
                        if (file != null)
                        {
                            await file.CopyToAsync(memoryStream, token);
                            CapturedImageBytes = memoryStream.ToArray();
                        }
                    }
                    this.RaisePropertyChanged(nameof(IsReadyToProcess));

                }
                else
                    await Parent.Alert.Handle("Permissions required").GetAwaiter();
            }
            catch (Exception ex)
            {
                await Parent.Alert.Handle(ex.Message).GetAwaiter();
            }
        }
        protected async Task DoPickPhoto(CancellationToken token = default)
        {
            try
            {
                Reset();
                using (var memoryStream = new MemoryStream())
                {
                    using var file = await Parent.OpenFile.Handle("Pick an image with Runes").GetAwaiter();
                    if (file != null)
                    {
                        await file.CopyToAsync(memoryStream, token);
                        CapturedImageBytes = memoryStream.ToArray();
                    }
                }
                this.RaisePropertyChanged(nameof(IsReadyToProcess));
            }
            catch (Exception ex)
            {
                await Parent.Alert.Handle(ex.Message);
            }
        }
        protected async Task ProcessImageRequest(byte[] fileBytes, CancellationToken token = default)
        {
            try
            {
                Parent.IsLoading = true;
                if (await Parent.HasPermissions.Handle("permissions").GetAwaiter())
                {
                    Reset();
                    using (SKBitmap sourceBitmap = SKBitmap.Decode(fileBytes))
                    {
                        int sourceWidth = sourceBitmap.Width;
                        int sourceHeight = sourceBitmap.Height;
                        var targetWidth = (sourceWidth > 512 ? 512 : sourceWidth);
                        // Calculate the aspect ratio to maintain the original image's proportions
                        float aspectRatio = (float)sourceWidth / sourceHeight;
                        var targetHeight = (int)(targetWidth / aspectRatio);
                        if (targetHeight > 512)
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
                    await Parent.Alert.Handle("Permissions required").GetAwaiter();
            }
            catch (Exception ex)
            {
                await Parent.Alert.Handle(ex.Message).GetAwaiter();
            }
            finally
            {
                Parent.IsLoading = false;
            }
        }
        protected async Task DoAskFuture(CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(Question))
            {
                await Parent.Alert.Handle("Please ask The Mystic a question.").GetAwaiter();
                return;
            }
            try
            {
                Parent.IsLoading = true;
                Answer = null;
                HasSaved = false;
                this.RaisePropertyChanged(nameof(CanSave));
                string answ = await ChatProxy.GetReading(this.Runes.ToArray(), this.Question, token);
                Answer = answ.Replace("\\n", "<br>");
                this.RaisePropertyChanged(nameof(CanSave));
            }
            catch (Exception ex)
            {
                await Parent.Alert.Handle(ex.Message).GetAwaiter();
            }
            finally
            {
                Parent.IsLoading = false;
            }
        }
        protected async Task DoProcessImage(CancellationToken token = default)
        {
            try
            {
                byte[]? image = null;
                using (var file = await Parent.SaveImage.Handle("Get cropped image").GetAwaiter())
                {
                    image = file?.ToArray();
                }
                if (image != null)
                    await ProcessImageRequest(image, token);
            }
            catch (Exception ex)
            {
                await Parent.Alert.Handle(ex.Message);
            }
        }
    }

}
