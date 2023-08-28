using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Devices;
using ReactiveUI;

namespace RyggRunes.MAUI.Client.Converters
{
    public class ByteToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value  == null) return null;
            if(value is byte[])
                return ImageSource.FromStream(() => new MemoryStream((byte[])value));
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    public class PixelScaler : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var screen = value as double?;
            if (screen == null || screen <= 0)
                screen = DeviceDisplay.MainDisplayInfo.Height;
            var scale = parameter as double?;

            if (scale == null)
                return screen.Value;
            return Math.Round(screen.Value * scale.Value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var screen = value as double?;
            if(screen == null || screen <= 0)
                screen = DeviceDisplay.MainDisplayInfo.Height;
            var scale = parameter as double?;

            if (scale == null)
                return screen.Value;
            return Math.Round((1 / scale.Value) * screen.Value);
        }
    }
}
