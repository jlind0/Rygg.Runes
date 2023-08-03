
using ReactiveUI;
using RyggRunes.Client.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Rygg.Runes.Client.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly Interaction<string, bool> hasPermissions;
        private readonly Interaction<string, bool> alert;
        private readonly Interaction<string, Stream> openFile;
        private byte[] annoatedImage;
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
        public Interaction<string, bool> HasPermissions => hasPermissions;
        public Interaction<string, bool> Alert => alert;
        public Interaction<string, Stream> OpenFile => openFile;
        protected IRunesProxy RunesProxy { get; }
        public ObservableCollection<string> Runes { get; } = new ObservableCollection<string>();
        public ICommand ProcessImage { get; }
        public MainWindowViewModel(IRunesProxy runesProxy) 
        {
            RunesProxy = runesProxy;
            hasPermissions = new Interaction<string, bool>();
            alert = new Interaction<string, bool>();
            openFile = new Interaction<string, Stream>();
            ProcessImage = ReactiveCommand.CreateFromTask(DoProcessImage);
        }
        protected async Task DoProcessImage(CancellationToken token = default)
        {
            this.IsLoading = true;
            if (await HasPermissions.Handle("permissions").GetAwaiter())
            {
                Runes.Clear();
                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    using var file = await OpenFile.Handle("Pick an image with Runes").GetAwaiter();
                    await file.CopyToAsync(memoryStream, token);
                    fileBytes = memoryStream.ToArray();
                }
                var resp = await RunesProxy.ProcessImage(fileBytes, token);
                if (resp != null)
                {
                    AnnotatedImage = resp.AnnotatedImage;
                    foreach (var rune in resp.Annotations)
                    {
                        Runes.Add(rune);
                    }
                }
            }
            else
                await Alert.Handle("Permissions required").GetAwaiter();
            this.IsLoading = false;
        }
    }
}
