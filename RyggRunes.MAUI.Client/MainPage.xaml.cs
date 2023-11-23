using Rygg.Runes.Client.ViewModels;
using RyggRunes.Client.Core;
using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Maui;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using System.Reactive.Disposables;
using Microsoft.Maui.Devices;
using CommunityToolkit.Maui.Markup;
using RyggRunes.MAUI.Client.Converters;

#if WINDOWS
using Windows.Media.Capture;
#endif
namespace RyggRunes.MAUI.Client
{
    public partial class MainPage : ReactiveContentPage<MainWindowViewModel>
    {
       
        public MainPage(MainWindowViewModel vm)
        {
            vm.Dispatcher = new MauiDispatcherProxy(Dispatcher);
            ViewModel = vm;
            InitializeComponent();
            BindingContext = ViewModel;
#if IOS || ANDROID
            ViewModel.ScreenHeight = DeviceDisplay.Current.MainDisplayInfo.Height;
            ViewModel.ScreenWidth = DeviceDisplay.Current.MainDisplayInfo.Width;
#else
            ViewModel.ScreenWidth = this.Width;
            ViewModel.ScreenHeight = this.Height;
#endif
            this.WhenActivated(d =>
            {
                this.WhenPropertyChanged(p => p.Width).Subscribe(p => 
                    ViewModel.ScreenWidth = p.Value).DisposeWith(d);
                this.WhenPropertyChanged(p => p.Height).Subscribe(p => 
                    ViewModel.ScreenHeight = p.Value).DisposeWith(d);
                ViewModel.Alert.RegisterHandler(async interaction =>
                {
                    await DisplayAlert("Alert", interaction.Input, "OK");
                    interaction.SetOutput(true);
                }).DisposeWith(d);
                ViewModel.OpenFile.RegisterHandler(async interaction =>
                {
                    try
                    {
                        var fileResult = await MediaPicker.PickPhotoAsync(new MediaPickerOptions()
                        {
                            Title = "Pick a Photo with Runes"
                        });
                        if (fileResult != null)
                            interaction.SetOutput(await fileResult.OpenReadAsync());
                        else
                            interaction.SetOutput(null);
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
                        interaction.SetOutput(await result.OpenStreamForReadAsync());
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
                                interaction.SetOutput(await photo.OpenReadAsync());
                            }
                            else
                                interaction.SetOutput(null);
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