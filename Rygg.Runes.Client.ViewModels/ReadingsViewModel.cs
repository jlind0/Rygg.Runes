using DynamicData;
using ReactiveUI;
using Rygg.Runes.Data.Embedded;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
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
        public ObservableCollection<ReadingViewModel> Readings { get; } = new ObservableCollection<ReadingViewModel>();
        public ICommand Load { get; }
        public ICommand Search { get; }
        public ICommand PageForward { get; }
        public ICommand PageBack { get; }
        private string? searchCondition;
        public string? SearchCondition
        {
            get => searchCondition;
            set => this.RaiseAndSetIfChanged(ref searchCondition, value);
        }
        private int pageSize = 5;
        public int PageSize
        {
            get => pageSize;
            set => this.RaiseAndSetIfChanged(ref pageSize, value);
        }
        private int page = 1;
        public int Page
        {
            get => page;
            set => this.RaiseAndSetIfChanged(ref page, value);
        }
        private long count;
        public long Count
        {
            get => count;
            set => this.RaiseAndSetIfChanged(ref count, value);
        }
        public bool CanPageForward
        {
            get => Page * PageSize < Count;
        }
        public bool CanPageBack
        {
            get => Page > 1;
        }
        public ReadingsViewModel(MainWindowViewModel parent, IReadingsDataAdapter readingsDataAdapter)
        {
            Parent = parent;
            ReadingsDataAdapter = readingsDataAdapter;
            Load = ReactiveCommand.CreateFromTask(DoLoad);
            Search = ReactiveCommand.CreateFromTask(DoSearch);
            PageForward = ReactiveCommand.CreateFromTask(DoPageForward);
            PageBack = ReactiveCommand.CreateFromTask(DoPageBack);
        }
        protected async Task DoPageForward(CancellationToken token = default)
        {
            if (CanPageForward)
            {
                Page++;
                await DoLoad(token);
            }
        }
        protected async Task DoPageBack(CancellationToken token = default)
        {
            if (CanPageBack)
            {
                Page--;
                await DoLoad(token);
            }
        }
        protected async Task DoSearch(CancellationToken token = default)
        {
            Page = 1;
            await DoLoad(token);
        }
        protected async Task DoLoad(CancellationToken token = default)
        {
            try
            {
                Parent.IsLoading = true;
                if (isFirstLoad)
                    await ReadingsDataAdapter.CreateDatabase(token);
                isFirstLoad = false;
                Readings.Clear();
                Count = await ReadingsDataAdapter.Count(SearchCondition, token);
                await foreach(var reading in ReadingsDataAdapter.GetAll(PageSize, Page ,SearchCondition, token))
                {
                    Readings.Add(new ReadingViewModel(this, reading));
                }
                this.RaisePropertyChanged(nameof(CanPageBack));
                this.RaisePropertyChanged(nameof(CanPageForward));
            }
            catch(Exception ex)
            {
                await Parent.Alert.Handle(ex.Message).GetAwaiter();
            }
            finally
            {
                Parent.IsLoading = false;
            }
        }
    }
}
