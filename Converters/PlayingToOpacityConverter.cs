using System.Globalization;
using System.Windows.Data;

namespace MidiStepExtractor.Converters
{
    public class PlayingToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value is true ? 1.0 : 0.0;
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }
}
