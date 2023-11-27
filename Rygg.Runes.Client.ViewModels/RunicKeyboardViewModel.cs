using ReactiveUI;
using Rygg.Runes.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Drawing;
using System.Reactive.Linq;

namespace Rygg.Runes.Client.ViewModels
{
    public class RunicKeyboardViewModel : ReactiveObject
    {
        public RuneItem Parent { get; }
        private bool isOpen = false;
        public bool IsOpen
        {
            get => isOpen;
            set => this.RaiseAndSetIfChanged(ref isOpen, value);
        }
        public ICommand OpenOrClose { get; }
        public ReactiveCommand<Rune, PlacedRune> SelectRune { get; }
        public RuneKeyViewModel[] Runes { get; }
        public RunicKeyboardViewModel(RuneItem parent)
        {
            Parent = parent;
            Runes = Rune.Alphabet.Select(r => new RuneKeyViewModel(r, this)).ToArray();
            SelectRune = ReactiveCommand.CreateFromTask<Rune, PlacedRune>(DoSelectRune);
            OpenOrClose = ReactiveCommand.CreateFromTask(DoOpenOrClose);
        }
        protected async Task<PlacedRune> DoSelectRune(Rune rune)
        {
            var r = new PlacedRune(rune.Name, new Point(Parent.Row, Parent.Column));
            await Parent.Parent.Parent.Parent.Dispatcher.Dispatch(() => Parent.Rune = r);
            await DoOpenOrClose();
            return r;
        }
        protected async Task DoOpenOrClose()
        {
            try
            {
                await Parent.Parent.Parent.Parent.Dispatcher.Dispatch(() => IsOpen = !IsOpen);
            }
            catch(Exception ex)
            {
                await Parent.Parent.Parent.Parent.Alert.Handle(ex.Message).GetAwaiter();
            }
        }
    }
    public class RuneKeyViewModel : ReactiveObject
    {
        public Rune Rune { get; }
        public RunicKeyboardViewModel Parent { get; }
        public RuneKeyViewModel(Rune rune, RunicKeyboardViewModel parent)
        {
            Rune = rune;
            Parent = parent;
        }   
    }
}
