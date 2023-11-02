using DynamicData;
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
using System.Threading.Tasks;
using System.Windows.Input;
using Rygg.Runes.Data.Core;
using System.Reactive;
using System.Reactive.Joins;
using System.Drawing;

namespace Rygg.Runes.Client.ViewModels
{
    public abstract class RuneStepViewModel : ReactiveObject
    {
        public abstract int StepNumber { get; }
        public RuneDetectorViewModel Parent { get; }
        private bool isActive = false;
        public ICommand MoveBack { get; }
        public bool CanMoveBack
        {
            get => StepNumber > 1;
        }
        public bool IsActive
        {
            get => isActive;
            set => this.RaiseAndSetIfChanged(ref isActive, value);
        }
        public RuneStepViewModel(RuneDetectorViewModel parent)
        {
            Parent = parent;
            MoveBack = ReactiveCommand.Create(DoMoveBack);
        }
        public void DoMoveBack() 
        {
            Parent.NavigateStep(StepNumber - 1);
        }
        public virtual void Reset(bool fromParent = false)
        {
        }
    }
    public class RuneAskUniverseStepViewModel : RuneStepViewModel
    {
        private string? question;
        public string? Question
        {
            get => question;
            set => this.RaiseAndSetIfChanged(ref question, value);
        }
        public ICommand AskFuture { get; }
        public RuneAskUniverseStepViewModel(RuneDetectorViewModel parent) : base(parent)
        {
            AskFuture = ReactiveCommand.Create(DoAskFuture);
        }
        protected void DoAskFuture()
        {
            if (!string.IsNullOrWhiteSpace(Question))
            {
                Parent.NavigateStep(Parent.SpreadsVM.StepNumber);
            }
            
        }
        public override int StepNumber => 1;
        public override void Reset(bool fromParent = false)
        {
            Question = null;
            base.Reset(fromParent);
        }
    }
    public class RunesSelectImageViewModel : RuneStepViewModel
    {
        public ICommand TakePhoto { get; }
        public ICommand PickPhoto { get; }
        protected IRunesProxy RunesProxy { get; }
        public ICommand ProcessImage { get; }
        public ICommand PickRandomRunes { get; }
        public ICommand PickYourOwnRunes { get; }
        public bool IsReadyToProcess
        {
            get => CapturedImageBytes != null;
        }
        private byte[]? capturedImageBytes;
        public byte[]? CapturedImageBytes
        {
            get => capturedImageBytes;
            set => this.RaiseAndSetIfChanged(ref capturedImageBytes, value);
        }
        private byte[]? annoatedImage;
        public byte[]? AnnotatedImage
        {
            get => annoatedImage;
            set => this.RaiseAndSetIfChanged(ref annoatedImage, value);
        }
        public RunesSelectImageViewModel(RuneDetectorViewModel parent, IRunesProxy runesProxy) : base(parent)
        {
            RunesProxy = runesProxy;
            TakePhoto = ReactiveCommand.CreateFromTask(DoTakePhoto);
            PickPhoto = ReactiveCommand.CreateFromTask(DoPickPhoto);
            ProcessImage = ReactiveCommand.CreateFromTask(DoProcessImage);
            PickRandomRunes = ReactiveCommand.Create(DoPickRandomRunes);
            PickYourOwnRunes = ReactiveCommand.Create(DoPickYourOwnRunes);
        }
        protected void DoPickYourOwnRunes()
        {
            Parent.RunesDetectedVM.SetRunesDetected(Array.Empty<PlacedRune>());
            Parent.NavigateStep(StepNumber + 1);
        }
        protected void DoPickRandomRunes()
        {
            var runes = Rune.Alphabet;
            var validMatrix = Parent.SpreadsVM.SelectedSpread.Spread.ValidMatrix;
            int m = 0, rowCount = validMatrix.GetLength(0), columnCount = validMatrix.GetLength(1);
            List<PlacedRune> lst = new List<PlacedRune>();
            while(m < rowCount)
            {
                int n = 0;
                while(n < columnCount)
                {
                    if (validMatrix[m, n])
                    {

                        Random rand = new Random(m + n);
                        PlacedRune pr;
                        do
                        {
                            pr = new PlacedRune(runes[rand.Next(0, runes.Length - 1)].Name, new Point((n + 1) * 75, (m + 1) * 75));
                        }
                        while(lst.Any(r => r.Name == pr.Name));
                        lst.Add(pr);
                    }
                    n++;
                }
                m++;
            }
            Parent.RunesDetectedVM.SetRunesDetected(lst.ToArray());
            Parent.NavigateStep(StepNumber + 1);
        }
        protected async Task DoTakePhoto(CancellationToken token = default)
        {
            try
            {
                Reset();

                if (await Parent.Parent.HasPermissions.Handle("permissions"))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using var file = await Parent.Parent.CaptureWithCamera.Handle("Pick an image with Runes").GetAwaiter();
                        if (file != null)
                        {
                            await file.CopyToAsync(memoryStream, token);
                            CapturedImageBytes = memoryStream.ToArray();
                        }
                    }
                    this.RaisePropertyChanged(nameof(IsReadyToProcess));

                }
                else
                    await Parent.Parent.Alert.Handle("Permissions required").GetAwaiter();
            }
            catch (Exception ex)
            {
                await Parent.Parent.Alert.Handle(ex.Message).GetAwaiter();
            }
        }
        protected async Task DoPickPhoto(CancellationToken token = default)
        {
            try
            {
                Reset();
                using (var memoryStream = new MemoryStream())
                {
                    using var file = await Parent.Parent.OpenFile.Handle("Pick an image with Runes").GetAwaiter();
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
                await Parent.Parent.Alert.Handle(ex.Message);
            }
        }
        protected async Task ProcessImageRequest(byte[] fileBytes, bool isFirst = true, CancellationToken token = default)
        {
            try
            {
                Parent.Parent.IsLoading = true;
                if (await Parent.Parent.HasPermissions.Handle("permissions").GetAwaiter())
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
                        Parent.RunesDetectedVM.SetRunesDetected(resp.Annotations.Select(r => new PlacedRune(r)).ToArray());
                        Parent.NavigateStep(Parent.RunesDetectedVM.StepNumber);
                        this.RaisePropertyChanged(nameof(IsReadyToProcess));
                    }
                }
                else
                    await Parent.Parent.Alert.Handle("Permissions required").GetAwaiter();
            }
            catch (MsalException ex)
            {
                if (isFirst)
                {
                    await Parent.Parent.DoLogin(true, token);
                    await ProcessImageRequest(fileBytes, false, token);
                }
                else
                    await Parent.Parent.Alert.Handle(ex.Message).GetAwaiter();
            }
            catch (Exception ex)
            {
                await Parent.Parent.Alert.Handle(ex.StackTrace ?? "").GetAwaiter();
                await Parent.Parent.Alert.Handle(ex.Message).GetAwaiter();
            }
            finally
            {
                Parent.Parent.IsLoading = false;
            }
        }
        protected async Task DoProcessImage(CancellationToken token = default)
        {
            try
            {
                if(CapturedImageBytes != null)
                    await ProcessImageRequest(CapturedImageBytes, token: token);
            }
            catch (Exception ex)
            {
                await Parent.Parent.Alert.Handle(ex.Message);
            }
        }
        public override int StepNumber => 3;
        public override void Reset(bool fromParent = false)
        {
            CapturedImageBytes = null;
            AnnotatedImage = null;
            this.RaisePropertyChanged(nameof(IsReadyToProcess));
            base.Reset(fromParent);
        }

    }
    public class RuneRow : ReactiveObject
    {
        public RuneItem[] Runes { get; }
        public RunesDetectedViewModel Parent { get; }
        public RuneRow(RuneItem[] runes, RunesDetectedViewModel parent)
        {
            Parent = parent;
            Runes = runes;
        }
    }
    public class RuneItem : ReactiveObject
    {
        public RunicKeyboardViewModel KeyboardViewModel { get; }
        private PlacedRune? rune;
        public PlacedRune? Rune
        {
            get => rune;
            set
            {
                this.RaiseAndSetIfChanged(ref rune, value);
                this.RaisePropertyChanged(nameof(RunicCharachter));
                this.RaisePropertyChanged(nameof(RuneName));
            }
        }
        
        public string? RunicCharachter
        {
            get => Rune?.RunicCharachter;
        }
        public string? RuneName
        {
            get => Rune?.Name;
        }
        private readonly bool _isValidSlot;
        public bool IsValidSlot
        {
            get => _isValidSlot;
        }
        public bool IsBlank { get => Rune == null; }
        public int Row { get; }
        public int Column { get; }
        public RunesDetectedViewModel Parent { get; }

        public RuneItem(PlacedRune? rune, int row, int column, bool isValidSlot, RunesDetectedViewModel parent)
        {
            KeyboardViewModel = new RunicKeyboardViewModel(this);
            Row = row;
            Column = column;
            Rune = rune;
            _isValidSlot = isValidSlot;
            Parent = parent;
        }
    }
    public class RunesDetectedViewModel : RuneStepViewModel
    {
        
        private string? answer;
        public string? Answer
        {
            get => answer;
            set => this.RaiseAndSetIfChanged(ref answer, value);
        }
        private RuneRow[]? runeRows;
        public RuneRow[]? RuneRows
        {
            get => runeRows;
            set { 
                this.RaiseAndSetIfChanged(ref runeRows, value);
                this.RaisePropertyChanged(nameof(SelectedRunes));
            }
        }
        public PlacedRune[] SelectedRunes
        {
            get
            {
                List<PlacedRune> runes = new List<PlacedRune>();
                if (RuneRows != null)
                {
                    foreach (var row in RuneRows)
                    {
                        foreach (var rune in row.Runes.Where(r => r.Rune != null))
                        {
                            var r = rune.Rune ?? throw new InvalidDataException();
                            r.X1 = (1 + rune.Column) * 75;
                            r.X2 = (1 + rune.Column) * 75;
                            r.Y1 = (1 + rune.Row) * 75;
                            r.Y2 = (1 + rune.Row) * 75;
                            runes.Add(r);
                        }
                    }
                }
                return runes.ToArray();
            }
        }
        protected IChatGPTProxy ChatProxy { get; }
        public ICommand AskTheFuture { get; }
        
        protected async Task DoAskFuture(bool isFirst = true, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(Parent.AskTheUniverseVM.Question))
            {
                await Parent.Parent.Alert.Handle("Please ask The Mystic a question.").GetAwaiter();
                return;
            }
            try
            {
                Parent.Parent.IsLoading = true;
                Answer = null;
                string answ = await ChatProxy.GetReading(Parent.RunesDetectedVM.SelectedRunes,
                    Parent.SpreadsVM.SelectedSpread.Spread.Type,
                    Parent.AskTheUniverseVM.Question, token);
                Answer = answ.Replace("\\n", "<br>");
                Parent.NavigateStep(5);
            }
            catch (MsalClientException ex)
            {
                if (isFirst)
                {
                    await Parent.Parent.DoLogin(true, token);
                    await DoAskFuture(false, token);
                }
                else
                    await Parent.Parent.Alert.Handle(ex.Message).GetAwaiter();
            }
            catch (Exception ex)
            {
                await Parent.Parent.Alert.Handle(ex.Message).GetAwaiter();
            }
            finally
            {
                Parent.Parent.IsLoading = false;
            }
        }
        public RunesDetectedViewModel(RuneDetectorViewModel parent, IChatGPTProxy chatProxy) : base(parent)
        {
            ChatProxy = chatProxy;
            AskTheFuture = ReactiveCommand.CreateFromTask<bool>(DoAskFuture);
            
        }
        public void SetRunesDetected(PlacedRune[] runes)
        {
            RuneRows = null;
            Parent.SpreadsVM.SelectedSpread.Spread.Validate(runes, out PlacedRune?[,] matrix);
            List<RuneRow> rows = new();
            int j = 0, rowCount = matrix.GetLength(0), columnCount = matrix.GetLength(1);
            while(j < rowCount)
            {
                List<RuneItem> items = new List<RuneItem>();
                int i = 0;
                while(i < columnCount)
                {
                    PlacedRune? rune = matrix[j, i];
                    items.Add(new RuneItem(rune, j, i, Parent.SpreadsVM.SelectedSpread.Spread.ValidMatrix[j, i], this));
                    i++;
                }
                rows.Add(new RuneRow(items.ToArray(), this));
                j++;
            }
            RuneRows = rows.ToArray();
        }
        public override int StepNumber => 4;
        public override void Reset(bool fromParent = false)
        {
            Answer = null;
            base.Reset(fromParent);
        }
    }
    public class RuneReadingViewModel : RuneStepViewModel
    {

       
        public bool CanSave
        {
            get => !string.IsNullOrWhiteSpace(Parent.RunesDetectedVM.Answer) && 
                !string.IsNullOrWhiteSpace(Parent.AskTheUniverseVM.Question) 
                && !HasSaved;
        }
        public ICommand SaveReading { get; }
        public RuneReadingViewModel(RuneDetectorViewModel parent, IReadingsDataAdapter readingsDataAdapter) : base(parent)
        {
            SaveReading = ReactiveCommand.CreateFromTask(DoSaveReading);
            ReadingsAdapter = readingsDataAdapter;
        }
        private bool hasSaved = false;
        public bool HasSaved
        {
            get => hasSaved;
            set => this.RaiseAndSetIfChanged(ref hasSaved, value);
        }
        
        protected IReadingsDataAdapter ReadingsAdapter { get; }
        protected async Task DoSaveReading(CancellationToken token = default)
        {
            try
            {
                if (CanSave)
                {
                    Parent.Parent.IsLoading = true;
                    await ReadingsAdapter.Add(new Reading()
                    {
                        AnnotatedImage = Parent.SelectImageVM.AnnotatedImage ?? throw new InvalidDataException(),
                        Runes = Parent.RunesDetectedVM.SelectedRunes,
                        Question = Parent.AskTheUniverseVM.Question ?? throw new InvalidDataException(),
                        Answer = Parent.RunesDetectedVM.Answer ?? throw new InvalidDataException()

                    }, token);
                    HasSaved = true;
                    this.RaisePropertyChanged(nameof(CanSave));
                    await Parent.Parent.ReadingsVM.DoLoad(token);
                }
            }
            catch (Exception ex)
            {
                await Parent.Parent.Alert.Handle(ex.Message).GetAwaiter();
            }
            Parent.Parent.IsLoading = false;
        }
        public override int StepNumber => 5;
    }
    public class RuneDetectorViewModel : ReactiveObject
    {
        public Dictionary<int, RuneStepViewModel> RuneSteps { get; } = new Dictionary<int, RuneStepViewModel>();
        private RuneStepViewModel[] currentStep;
        public RuneStepViewModel[] CurrentStep
        {
            get => currentStep;
            set => this.RaiseAndSetIfChanged(ref currentStep, value);
        }
        public int CurrentStepNumber
        {
            get => CurrentStep.First().StepNumber;
        }
        

        public void NavigateStep(int stepNumber)
        {
            CurrentStep = new RuneStepViewModel[] { RuneSteps[stepNumber] };
            this.RaisePropertyChanged(nameof(CurrentStepNumber));
        }
        
        
        public MainWindowViewModel Parent { get; }
        protected IRunesProxy RunesProxy { get; }
        protected IChatGPTProxy ChatProxy { get; }
        protected IConfiguration Config { get; }
        protected IReadingsDataAdapter ReadingsAdapter { get; }
        
        public ICommand StartOver { get; }
        public RuneAskUniverseStepViewModel AskTheUniverseVM { get; }
        public RuneSpreadsViewModel SpreadsVM { get; }
        public RunesSelectImageViewModel SelectImageVM { get; }
        public RuneReadingViewModel ReadingVM { get; }
        public RunesDetectedViewModel RunesDetectedVM { get; }
        public RuneDetectorViewModel(MainWindowViewModel parent, IRunesProxy runesProxy, IChatGPTProxy chatProxy,
            IConfiguration config,
            IReadingsDataAdapter readingsAdapter)
        {
            Parent = parent;
            RunesProxy = runesProxy;
            ChatProxy = chatProxy;
            Config = config;
            ReadingsAdapter = readingsAdapter;
            StartOver = ReactiveCommand.Create(DoStartOver);


            AskTheUniverseVM = new RuneAskUniverseStepViewModel(this);
            SpreadsVM = new RuneSpreadsViewModel(this);
            SelectImageVM = new RunesSelectImageViewModel(this, RunesProxy);
            RunesDetectedVM = new RunesDetectedViewModel(this, ChatProxy);
            ReadingVM = new RuneReadingViewModel(this, readingsAdapter);
            
            RuneSteps.Add(AskTheUniverseVM.StepNumber, AskTheUniverseVM);
            RuneSteps.Add(SpreadsVM.StepNumber, SpreadsVM);
            RuneSteps.Add(SelectImageVM.StepNumber, SelectImageVM);
            RuneSteps.Add(RunesDetectedVM.StepNumber, RunesDetectedVM);
            RuneSteps.Add(ReadingVM.StepNumber, ReadingVM);
            currentStep = new RuneStepViewModel[] { RuneSteps[1] };

        }

        protected void DoStartOver()
        {
            Reset();
            NavigateStep(1);
        }
        public void Reset()
        {
            foreach (var vm in RuneSteps.Values)
                vm.Reset(true);
        }

        
        
    }

}
