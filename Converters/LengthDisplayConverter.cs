using System.Globalization;
using System.Windows.Data;

namespace MidiStepExtractor.Converters
{
    public class LengthDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            if (value is not double d) return "1";
            return d switch
            {
                <= 0.26 => "1/4",
                <= 0.51 => "1/2",
                <= 1.01 => "1",
                <= 2.01 => "2",
                _ => $"{d:0.#}"
            };
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }
}
