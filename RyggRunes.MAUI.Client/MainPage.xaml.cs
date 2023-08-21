using Rygg.Runes.Client.ViewModels;
using RyggRunes.Client.Core;
using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Maui;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using System.Reactive.Disposables;
using RyggRunes.MAUI.Client.Dispatcher;
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
            vm.DispatcherService = new MauiDispatcher(Dispatcher);
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
                    try
                    {
                        var fileResult = await FilePicker.PickAsync(new PickOptions
                        {
                            FileTypes = FilePickerFileType.Images,
                            PickerTitle = "Select an image"
                        });
                        if (fileResult != null)
                            interaction.SetOutput(await fileResult.OpenReadAsync());
                    }
                    catch (Exception ex) 
                    {
                        await DisplayAlert("Alert", ex.Message, "Ok");
                    }
                }).DisposeWith(d);
                ViewModel.HasPermissions.RegisterHandler(async interaction =>
                {
                    try
                    {
                        var status = await Permissions.RequestAsync<Permissions.StorageRead>();
                        var cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                        interaction.SetOutput(status == PermissionStatus.Granted && cameraStatus == PermissionStatus.Granted);
                    }
                    catch(Exception ex)
                    {
                        await DisplayAlert("Alert", ex.Message, "Ok");
                    }
                }).DisposeWith(d);
                ViewModel.CaptureWithCamera.RegisterHandler(async interaction =>
                {
#if WINDOWS
                    var captureUi = new RyggRunes.MAUI.Client.WinUI.CustomCameraCaptureUI();
                    var result = await captureUi.CaptureFileAsync(CameraCaptureUIMode.Photo);
                    if (result != null && result.IsAvailable)
                    {
                        using (var stream = await result.OpenStreamForReadAsync())
                        using (var ms = new MemoryStream())
                        {
                            await stream.CopyToAsync(ms);
                            interaction.SetOutput(ms.ToArray());
                        }
                    }
#else
                    try
                    {
                        if (MediaPicker.IsCaptureSupported)
                        {
                            var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
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
                    }
                    catch(Exception ex)
                    {
                        await DisplayAlert("Alert", ex.Message, "Ok");
                    }
#endif
                }).DisposeWith(d);
            });
            
        }
    }
}