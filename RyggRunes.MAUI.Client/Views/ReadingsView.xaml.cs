using ReactiveUI.Maui;
using Rygg.Runes.Client.ViewModels;
using System.Reactive.Linq;

namespace RyggRunes.MAUI.Client.Views;

public partial class ReadingsView : ReactiveContentView<ReadingsViewModel>
{
	public ReadingsView()
	{
		InitializeComponent();
	}

}