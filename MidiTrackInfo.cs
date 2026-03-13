namespace MidiStepExtractor.Models
{
    /// <summary>
    /// Summary information about one track in a loaded MIDI file.
    /// </summary>
    public class MidiTrackInfo
    {
        public int TrackIndex { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Channel { get; init; }           // -1 = mixed/unknown
        public int NoteCount { get; init; }
        public bool IsDrumTrack { get; init; }      // channel 9 = GM drums
        public int ProgramNumber { get; init; }     // GM instrument 0-127
        public string InstrumentName { get; init; } = string.Empty;
        public long TotalTicks { get; init; }
        public int BarCount { get; init; }

        public string DisplayName => string.IsNullOrWhiteSpace(Name)
            ? $"Track {TrackIndex + 1} ({InstrumentName})"
            : $"Track {TrackIndex + 1}: {Name} ({InstrumentName})";

        public override string ToString() => DisplayName;
    }
}
