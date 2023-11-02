using ReactiveUI;
using Rygg.Runes.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Drawing;

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
            SelectRune = ReactiveCommand.Create<Rune, PlacedRune>(DoSelectRune);
            OpenOrClose = ReactiveCommand.Create(DoOpenOrClose);
        }
        public PlacedRune DoSelectRune(Rune rune)
        {
            var r = new PlacedRune(rune.Name, new Point(Parent.Row, Parent.Column));
            Parent.Rune = r;
            this.DoOpenOrClose();
            return r;
        }
        public void DoOpenOrClose()
        {
            IsOpen = !IsOpen;
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
