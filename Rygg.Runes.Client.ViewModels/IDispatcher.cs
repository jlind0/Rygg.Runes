using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rygg.Runes.Client.ViewModels
{
    public interface IDispatcherProxy
    {
        Task Dispatch(Func<Task> action);
        Task Dispatch(Action action);
    }
}
