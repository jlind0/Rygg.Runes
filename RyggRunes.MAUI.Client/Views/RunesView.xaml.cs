using DynamicData.Binding;
using ReactiveUI.Maui;
using Rygg.Runes.Client.ViewModels;
using System.Reactive.Disposables;

namespace RyggRunes.MAUI.Client.Views;

public partial class RunesView : ReactiveContentView<RuneDetectorViewModel>
{
    public RunesView()
	{
		InitializeComponent();
    }
}