using Microsoft.Identity.Client;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Rygg.Runes.Client.ViewModels
{
    public class AdminViewModel : ReactiveObject
    {
        public MainWindowViewModel Parent { get; }
        public ICommand Logout { get; }
        protected IPublicClientApplication App { get; }
        public AdminViewModel(MainWindowViewModel parent, IPublicClientApplication app) 
        {
            Parent = parent;
            App = app;
            Logout = ReactiveCommand.CreateFromTask(DoLogout);
        }
        protected async Task DoLogout()
        {
            try
            {
                foreach(var acct in await App.GetAccountsAsync(Parent.SignInSignOutPolicy))
                {
                    await App.RemoveAsync(acct);
                }
                Parent.IsLoggedIn = false;
            }
            catch(Exception ex)
            {
                await Parent.Alert.Handle(ex.Message).GetAwaiter();
            }
        }
    }
}
