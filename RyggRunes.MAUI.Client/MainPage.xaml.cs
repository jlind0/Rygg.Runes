using Rygg.Runes.Client.ViewModels;
using RyggRunes.Client.Core;
using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Maui;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using System.Reactive.Disposables;
#if WINDOWS
using Windows.Media.Capture;
#endif
namespace RyggRunes.MAUI.Client
{
    public partial class MainPage : ReactiveContentPage<MainWindowViewModel>
    {
        public MainPage(MainWindowViewModel vm)
        {
            
            ViewModel = vm;
            InitializeComponent();
            BindingContext = ViewModel;
            this.WhenActivated(d =>
            {
                ViewModel.Alert.RegisterHandler(async interaction =>
                {
                    await DisplayAlert("Alert", interaction.Input, "OK");
                    interaction.SetOutput(true);
                }).DisposeWith(d);
                ViewModel.OpenFile.RegisterHandler(async interaction =>
                {
                    var fileResult = await FilePicker.PickAsync(new PickOptions
                    {
                        FileTypes = FilePickerFileType.Images,
                        PickerTitle = "Select an image"
                    });
                    if (fileResult != null)
                        interaction.SetOutput(await fileResult.OpenReadAsync());
                }).DisposeWith(d);
                ViewModel.HasPermissions.RegisterHandler(async interaction =>
                {
                    var status = await Permissions.RequestAsync<Permissions.StorageRead>();
                    var cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                    interaction.SetOutput(status == PermissionStatus.Granted && cameraStatus == PermissionStatus.Granted);
                }).DisposeWith(d);
                ViewModel.CaptureWithCamera.RegisterHandler(async interaction =>
                {
#if WINDOWS
                    var captureUi = new RyggRunes.MAUI.Client.WinUI.CustomCameraCaptureUI();
                    var result = await captureUi.CaptureFileAsync(CameraCaptureUIMode.Photo);
                    if (result != null)
                    {
                        using (var stream = await result.OpenStreamForReadAsync())
                        using (var ms = new MemoryStream())
                        {
                            await stream.CopyToAsync(ms);
                            interaction.SetOutput(ms.ToArray());
                        }
                    }
#else
                    if (MediaPicker.IsCaptureSupported)
                    {
                        var photo = await MediaPicker.Default.CaptureVideoAsync(new MediaPickerOptions
                        {
                            Title = "Take a Photo"
                        });

                        if (photo != null)
                        {
                            using (var stream = await photo.OpenReadAsync())
                            using (var memoryStream = new MemoryStream())
                            {
                                await stream.CopyToAsync(memoryStream);
                                interaction.SetOutput(memoryStream.ToArray());
                            }
                        }
                    }
#endif
                }).DisposeWith(d);
            });
            
        }
    }
}