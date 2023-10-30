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
    public class RuneViewTemplateSelector : DataTemplateSelector
    {
        public DataTemplate AskTheUniverseTemplate { get; set; }
        public DataTemplate SelectSpreadTemplate { get; set; }
        public DataTemplate SelectImageTemplate { get; set; }
        public DataTemplate DetectRunesTemplate { get; set; }
        public DataTemplate RunesReadingTemplate { get; set; }
        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            Type type = item.GetType();
            if (type == typeof(RuneAskUniverseStepViewModel)) return AskTheUniverseTemplate;
            else if (type == typeof(RuneSpreadsViewModel)) return SelectSpreadTemplate;
            else if (type == typeof(RunesSelectImageViewModel)) return SelectImageTemplate;
            else if (type == typeof(RunesDetectedViewModel)) return DetectRunesTemplate;
            else if (type == typeof(RuneReadingViewModel)) return RunesReadingTemplate;
            else throw new NotImplementedException();

        }
    }
}
