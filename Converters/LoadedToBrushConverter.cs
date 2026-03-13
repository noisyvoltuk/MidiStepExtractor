using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MidiStepExtractor.Converters
{
    public class LoadedToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush Ok  = new(Color.FromRgb(0x22, 0xC5, 0x5E));
        private static readonly SolidColorBrush Bad = new(Color.FromRgb(0xEF, 0x44, 0x44));

        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value is true ? Ok : Bad;
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }
}
