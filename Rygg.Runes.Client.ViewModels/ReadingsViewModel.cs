using DynamicData;
using ReactiveUI;
using Rygg.Runes.Data.Embedded;
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
    public class ReadingsViewModel : ReactiveObject
    {
        private bool isFirstLoad = true;
        public MainWindowViewModel Parent { get; }
        protected IReadingsDataAdapter ReadingsDataAdapter { get; }
        public ObservableCollection<Reading> Readings { get; } = new ObservableCollection<Reading>();
        public ICommand Load { get; }
        private string? searchCondition;
        public string? SearchCondition
        {
            get => searchCondition;
            set => this.RaiseAndSetIfChanged(ref searchCondition, value);
        }
        public ReadingsViewModel(MainWindowViewModel parent, IReadingsDataAdapter readingsDataAdapter)
        {
            Parent = parent;
            ReadingsDataAdapter = readingsDataAdapter;
            Load = ReactiveCommand.CreateFromTask(DoLoad);
        }
        public async Task DoLoad(CancellationToken token = default)
        {
            try
            {
                if (isFirstLoad)
                    await ReadingsDataAdapter.CreateDatabase(token);
                isFirstLoad = false;
                Readings.Clear();
                await foreach(var reading in ReadingsDataAdapter.GetAll(SearchCondition, token))
                {
                    Readings.Add(reading);
                }
            }
            catch(Exception ex)
            {
                await Parent.Alert.Handle(ex.Message).GetAwaiter();
            }
        }
    }
}
