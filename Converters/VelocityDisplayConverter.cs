using System.Globalization;
using System.Windows.Data;

namespace MidiStepExtractor.Converters
{
    public class VelocityDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value is int v ? $"V{v}" : "V?";
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }
}
