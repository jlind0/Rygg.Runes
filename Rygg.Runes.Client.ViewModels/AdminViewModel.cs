using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rygg.Runes.Client.ViewModels
{
    public class AdminViewModel : ReactiveObject
    {
        public MainWindowViewModel Parent { get; }
        public AdminViewModel(MainWindowViewModel parent) 
        {
            Parent = parent;
        }
    }
}
