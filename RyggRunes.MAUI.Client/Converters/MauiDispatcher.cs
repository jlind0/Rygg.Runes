using Rygg.Runes.Client.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RyggRunes.MAUI.Client.Converters
{
    public class MauiDispatcherProxy : IDispatcherProxy
    {
        protected IDispatcher Dispatcher { get; }
        public MauiDispatcherProxy(IDispatcher dispatcher)
        {
            Dispatcher = dispatcher;
        }
        public Task Dispatch(Func<Task> action)
        {
            return Dispatcher.DispatchAsync(action);
        }

        public Task Dispatch(Action action)
        {
            return Dispatcher.DispatchAsync(action);
        }
    }
}
