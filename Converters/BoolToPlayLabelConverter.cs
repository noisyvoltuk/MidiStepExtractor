using System.Globalization;
using System.Windows.Data;

namespace MidiStepExtractor.Converters
{
    public class BoolToPlayLabelConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value is true ? "⏹  STOP" : "▶  PLAY";
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }
}
