using DynamicData.Binding;
using ReactiveUI;
using Rygg.Runes.Spreads;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rygg.Runes.Client.ViewModels
{
    public class RuneSpreadsViewModel : RuneStepViewModel
    {
        public override int StepNumber => 2;
        public ObservableCollection<RuneSpreadViewModel> Spreads { get; } = new ObservableCollection<RuneSpreadViewModel>();
        private RuneSpreadViewModel selectedSpread;
        public ReactiveCommand<RuneSpreadViewModel, Unit> SelectSpread { get; }
        public RuneSpreadViewModel SelectedSpread
        {
            get => selectedSpread;
            set => this.RaiseAndSetIfChanged(ref selectedSpread, value);
        }
        public RuneSpreadsViewModel(RuneDetectorViewModel parent) :base(parent)
        {
            SelectSpread = ReactiveCommand.Create<RuneSpreadViewModel>(DoSelectSpread);
            Spreads.Add(new RuneSpreadViewModel<AstrologicalSpread>(this));
            Spreads.Add(new RuneSpreadViewModel<ChoiceSpread>(this));
            Spreads.Add(new RuneSpreadViewModel<SimpleLoveSpread>(this));
            Spreads.Add(new RuneSpreadViewModel<CurrentRelationshipSpread>(this));
            Spreads.Add(new RuneSpreadViewModel<YesNoSpread>(this));
            Spreads.Add(new RuneSpreadViewModel<CelticCrossSpread>(this));
            Spreads.Add(new RuneSpreadViewModel<AnswerToWhySpread>(this));
            Spreads.Add(new RuneSpreadViewModel<NornsSpread>(this));
            Spreads.Add(new RuneSpreadViewModel<AdviceSpread>(this));
            Spreads.Add(new RuneSpreadViewModel<SevenGemsSpread>(this));
            Spreads.Add(new RuneSpreadViewModel<FiveCardSpread>(this));
            Spreads.Add(new RuneSpreadViewModel<DecisonSpread>(this));
            Spreads.Add(new RuneSpreadViewModel<FourCardSpread>(this));
            selectedSpread = Spreads.First();
        }
        protected void DoSelectSpread(RuneSpreadViewModel spread)
        {
            SelectedSpread = spread;
            Parent.NavigateStep(Parent.SelectImageVM.StepNumber);
        }
    }
    public class RuneSpreadItem
    {
        public bool HasCard { get; }
        public bool DoesNotHaveCard { get => !HasCard; }
        public RuneSpreadItem(bool hasCard)
        {
            HasCard = hasCard;
        }
    }
    public class RuneSpreadRow
    {
        public RuneSpreadItem[] Runes { get; }
        public RuneSpreadRow(RuneSpreadItem[] runes)
        {
            Runes = runes;
        }
    }
    public abstract class RuneSpreadViewModel : ReactiveObject
    {
        public RuneSpreadsViewModel Parent { get; }
        public abstract RuneSpreadRow[] RuneRows { get; }
        public abstract string Name { get; }
        public abstract Spread Spread { get; }
        public bool IsSelected
        {
            get => Parent.SelectedSpread == this;
        }
        public RuneSpreadViewModel(RuneSpreadsViewModel parent)
        {
            Parent = parent;
            Parent.WhenPropertyChanged(p => p.SelectedSpread).Subscribe(p => this.RaisePropertyChanged(nameof(IsSelected)));
        }
    }
    public class RuneSpreadViewModel<TSpread> : RuneSpreadViewModel
        where TSpread: Spread, new()
    {
        public override Spread Spread => new TSpread();
        public override string Name => Spread.Name;
        public override RuneSpreadRow[] RuneRows { get; }

        public RuneSpreadViewModel(RuneSpreadsViewModel parent) : base(parent)
        {
            List<RuneSpreadRow> rows = new List<RuneSpreadRow>();
            int rowCount = Spread.ValidMatrix.GetLength(0);
            int columnCount = Spread.ValidMatrix.GetLength(1);
            int row = 0;
            while(row < rowCount)
            {
                List<RuneSpreadItem> items = new List<RuneSpreadItem>();
                int column = 0;
                while(column < columnCount)
                {
                    items.Add(new RuneSpreadItem(Spread.ValidMatrix[row, column]));
                    column++;
                }
                rows.Add(new RuneSpreadRow(items.ToArray()));
                row++;
            }
            RuneRows = rows.ToArray();
        }
    }
}
