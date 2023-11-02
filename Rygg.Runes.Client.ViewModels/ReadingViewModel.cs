using ReactiveUI;
using Rygg.Runes.Data.Embedded;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rygg.Runes.Data.Core;

namespace Rygg.Runes.Client.ViewModels
{
    public class ReadingViewModel : ReactiveObject
    {
        public long Id { get => Data.Id; }
        public string Question { get => Data.Question; }
        public string Answer { get => Data.Answer; }
        public PlacedRune[] Runes { get => Data.Runes; }
        public byte[] AnnotatedImage { get => Data.AnnotatedImage; }
        protected Reading Data { get; }
        public ReadingsViewModel Parent { get; }
        public ReadingViewModel(ReadingsViewModel parent, Reading data) 
        { 
            Data = data;
            Parent = parent;
        }
    }
}
