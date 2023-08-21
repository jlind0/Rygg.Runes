using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rygg.Runes.Client.ViewModels;

namespace RyggRunes.MAUI.Client.Dispatcher
{
    public class MauiDispatcher : IDispatcherService
    {
        protected IDispatcher Dispatcher { get; }
        public MauiDispatcher(IDispatcher dispatcher)
        {
            Dispatcher = dispatcher;
        }
        public Task Dispatch(Action action)
        {
            return Dispatcher.DispatchAsync(action);
        }
    }
}
