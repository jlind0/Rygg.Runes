using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rygg.Runes.Client.ViewModels;

namespace RyggRunes.MAUI.Client.Converters
{
    public class ViewTemplateSelector : DataTemplateSelector
    {
        public DataTemplate HomeTemplate { get; set; }
        public DataTemplate AdminTemplate { get; set; }
        public DataTemplate HistoryTemplate { get; set; }
        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            Type type = item.GetType();
            if (type == typeof(ReadingsViewModel)) return HistoryTemplate;
            else if (type == typeof(AdminViewModel)) return AdminTemplate;
            else if (type == typeof(HomeViewModel)) return HomeTemplate;
            else throw new NotImplementedException();
        }
    }
}
