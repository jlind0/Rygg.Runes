using DynamicData.Binding;
using ReactiveUI.Maui;
using Rygg.Runes.Client.ViewModels;
using System.Reactive.Disposables;

namespace RyggRunes.MAUI.Client.Views;

public partial class RunesView : ReactiveContentView<RuneViewModel>
{
    public event EventHandler ImageDataChanged;
    public RunesView()
	{
		InitializeComponent();
        this.Loaded += DetectorView_Loaded;
    }

    private void DetectorView_Loaded(object sender, EventArgs e)
    {
        ViewModel.WhenPropertyChanged(p => p.CapturedImageBytes).Subscribe(async pv =>
        {
            await Task.Delay(500);
            ImageDataChanged?.Invoke(this, EventArgs.Empty);
        });
        ViewModel.Parent.SaveImage.RegisterHandler(async interaction =>
        {
            MemoryStream ms = new();
            await imgEditor.SaveAsync(ms, ImageFormat.Jpeg, 100);
            interaction.SetOutput(ms);
        });
    }
}