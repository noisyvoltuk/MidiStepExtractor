using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MidiStepExtractor.Converters
{
    public class StepColorConverter : IMultiValueConverter
    {
        private static readonly SolidColorBrush ActiveBrush   = new(Color.FromRgb(0xF5, 0x9E, 0x0B));
        private static readonly SolidColorBrush PlayBrush     = new(Color.FromRgb(0x22, 0xC5, 0x5E));
        private static readonly SolidColorBrush InactiveBrush = new(Color.FromRgb(0x1E, 0x1E, 0x28));
        private static readonly SolidColorBrush BeatBrush     = new(Color.FromRgb(0x2A, 0x2A, 0x3A));

        public object Convert(object[] values, Type t, object p, CultureInfo c)
        {
            bool isActive  = values.Length > 0 && values[0] is true;
            bool isPlaying = values.Length > 1 && values[1] is true;
            int  stepIdx   = values.Length > 2 && values[2] is int i ? i : 0;

            if (isPlaying) return PlayBrush;
            if (isActive)  return ActiveBrush;
            return stepIdx % 4 == 0 ? BeatBrush : InactiveBrush;
        }

        public object[] ConvertBack(object v, Type[] t, object p, CultureInfo c) => throw new NotImplementedException();
    }
}
