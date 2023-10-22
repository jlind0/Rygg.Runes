using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using ReactiveUI;
using Rygg.Runes.Data.Embedded;
using RyggRunes.Client.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rygg.Runes.Client.ViewModels
{
    public class HomeViewModel : ReactiveObject
    {
        public MainWindowViewModel Parent { get; }
        protected IRunesProxy RunesProxy { get; }
        protected IChatGPTProxy ChatGPTProxy { get; }
        protected IConfiguration Configuration { get; }
        protected IReadingsDataAdapter ReadingsDataAdapter { get; }
        public RuneViewModel RunesVM { get; }
        public PalmViewModel PalmVM { get; }
        public TarotViewModel TarotVM { get; }
        public PsychicViewModel PsychicVM { get; }

        public HomeViewModel(MainWindowViewModel parent, IRunesProxy runesProxy, IChatGPTProxy chatProxy,
            IConfiguration config,
            IReadingsDataAdapter readingsAdapter)
        {
            Parent = parent;
            RunesProxy = runesProxy;
            ChatGPTProxy = chatProxy;
            Configuration = config;
            ReadingsDataAdapter = readingsAdapter;
            RunesVM = new RuneViewModel(parent, runesProxy, chatProxy, config, readingsAdapter);
            PalmVM = new PalmViewModel();
            TarotVM = new TarotViewModel();
            PsychicVM = new PsychicViewModel();
        }
    }
    public class TarotViewModel : ReactiveObject { }
    public class PalmViewModel : ReactiveObject { }
    public class PsychicViewModel: ReactiveObject { }
}
