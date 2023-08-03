using Rygg.Runes.Client.ViewModels;
using RyggRunes.Client.Core;
using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Maui;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using System.Reactive.Disposables;

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
                    interaction.SetOutput(status == PermissionStatus.Granted);
                }).DisposeWith(d);
            });
            
        }
    }
}