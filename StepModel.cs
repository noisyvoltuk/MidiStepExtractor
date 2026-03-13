using CommunityToolkit.Mvvm.ComponentModel;

namespace MidiStepExtractor.Models
{
    /// <summary>
    /// Represents a single step in the 16/32-step grid.
    /// </summary>
    public partial class StepModel : ObservableObject
    {
        [ObservableProperty] private bool _isActive;
        [ObservableProperty] private int _noteNumber;    // MIDI note 0-127
        [ObservableProperty] private int _velocity;      // 0-127
        [ObservableProperty] private double _lengthTicks; // duration in ticks
        [ObservableProperty] private bool _isPlaying;    // playhead highlight

        public int StepIndex { get; init; }

        /// <summary>MIDI note number to human-readable name, e.g. 60 → C4</summary>
        public string NoteName => NoteNumberToName(NoteNumber);

        /// <summary>Shortened note display for grid cells</summary>
        public string ShortNoteName => NoteNumber >= 0 ? NoteName : "—";

        partial void OnNoteNumberChanged(int value) => OnPropertyChanged(nameof(NoteName));
        partial void OnNoteNumberChanged(int oldValue, int newValue) => OnPropertyChanged(nameof(ShortNoteName));

        public static string NoteNumberToName(int noteNumber)
        {
            if (noteNumber < 0 || noteNumber > 127) return "—";
            string[] noteNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
            int octave = (noteNumber / 12) - 1;
            return $"{noteNames[noteNumber % 12]}{octave}";
        }

        /// <summary>Length in 16th-note fractions (1.0 = one 16th note)</summary>
        public double LengthBeats => LengthTicks; // normalised externally

        public StepModel(int index)
        {
            StepIndex = index;
            IsActive = false;
            NoteNumber = 60;
            Velocity = 100;
            LengthTicks = 1.0;
        }
    }
}
