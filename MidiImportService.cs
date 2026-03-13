using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using MidiStepExtractor.Models;

namespace MidiStepExtractor.Services
{
    /// <summary>
    /// Reads MIDI files, enumerates tracks, and extracts step sequences.
    /// Uses DryWetMidi for robust MIDI parsing.
    /// </summary>
    public class MidiImportService
    {
        private MidiFile? _midiFile;
        private TempoMap? _tempoMap;

        // ──────────────────────────────────────────────────────────────────
        // GM instrument names (program 0-127)
        // ──────────────────────────────────────────────────────────────────
        private static readonly string[] GmInstruments =
        [
            "Acoustic Grand Piano","Bright Acoustic Piano","Electric Grand Piano","Honky-tonk Piano",
            "Electric Piano 1","Electric Piano 2","Harpsichord","Clavi","Celesta","Glockenspiel",
            "Music Box","Vibraphone","Marimba","Xylophone","Tubular Bells","Dulcimer",
            "Drawbar Organ","Percussive Organ","Rock Organ","Church Organ","Reed Organ","Accordion",
            "Harmonica","Tango Accordion","Acoustic Guitar (nylon)","Acoustic Guitar (steel)",
            "Electric Guitar (jazz)","Electric Guitar (clean)","Electric Guitar (muted)","Overdriven Guitar",
            "Distortion Guitar","Guitar Harmonics","Acoustic Bass","Electric Bass (finger)",
            "Electric Bass (pick)","Fretless Bass","Slap Bass 1","Slap Bass 2","Synth Bass 1","Synth Bass 2",
            "Violin","Viola","Cello","Contrabass","Tremolo Strings","Pizzicato Strings",
            "Orchestral Harp","Timpani","String Ensemble 1","String Ensemble 2","Synth Strings 1",
            "Synth Strings 2","Choir Aahs","Voice Oohs","Synth Choir","Orchestra Hit",
            "Trumpet","Trombone","Tuba","Muted Trumpet","French Horn","Brass Section",
            "Synth Brass 1","Synth Brass 2","Soprano Sax","Alto Sax","Tenor Sax","Baritone Sax",
            "Oboe","English Horn","Bassoon","Clarinet","Piccolo","Flute","Recorder","Pan Flute",
            "Blown Bottle","Shakuhachi","Whistle","Ocarina","Lead 1 (square)","Lead 2 (sawtooth)",
            "Lead 3 (calliope)","Lead 4 (chiff)","Lead 5 (charang)","Lead 6 (voice)","Lead 7 (fifths)",
            "Lead 8 (bass+lead)","Pad 1 (new age)","Pad 2 (warm)","Pad 3 (polysynth)","Pad 4 (choir)",
            "Pad 5 (bowed)","Pad 6 (metallic)","Pad 7 (halo)","Pad 8 (sweep)","FX 1 (rain)",
            "FX 2 (soundtrack)","FX 3 (crystal)","FX 4 (atmosphere)","FX 5 (brightness)","FX 6 (goblins)",
            "FX 7 (echoes)","FX 8 (sci-fi)","Sitar","Banjo","Shamisen","Koto","Kalimba","Bagpipe",
            "Fiddle","Shanai","Tinkle Bell","Agogo","Steel Drums","Woodblock","Taiko Drum",
            "Melodic Tom","Synth Drum","Reverse Cymbal","Guitar Fret Noise","Breath Noise",
            "Seashore","Bird Tweet","Telephone Ring","Helicopter","Applause","Gunshot"
        ];

        // ──────────────────────────────────────────────────────────────────

        public MidiFile? LoadedFile => _midiFile;
        public TempoMap? TempoMap => _tempoMap;
        public string FilePath { get; private set; } = string.Empty;

        /// <summary>Load a MIDI file and return track summaries.</summary>
        public List<MidiTrackInfo> LoadFile(string path)
        {
            FilePath = path;

            var readSettings = new ReadingSettings
            {
                NotEnoughBytesPolicy = NotEnoughBytesPolicy.Ignore,
                InvalidChannelEventParameterValuePolicy = InvalidChannelEventParameterValuePolicy.SnapToLimits,
                InvalidChunkSizePolicy = InvalidChunkSizePolicy.Ignore,
                InvalidMetaEventParameterValuePolicy = InvalidMetaEventParameterValuePolicy.SnapToLimits,
                MissedEndOfTrackPolicy = MissedEndOfTrackPolicy.Ignore,
                UnexpectedTrackChunksCountPolicy = UnexpectedTrackChunksCountPolicy.Ignore,
                UnknownChunkIdPolicy = UnknownChunkIdPolicy.Skip,
            };

            _midiFile = MidiFile.Read(path, readSettings);
            _tempoMap = _midiFile.GetTempoMap();
            return GetTrackInfos();
        }

        private List<MidiTrackInfo> GetTrackInfos()
        {
            var infos = new List<MidiTrackInfo>();
            if (_midiFile == null) return infos;

            int idx = 0;
            foreach (var track in _midiFile.Chunks.OfType<TrackChunk>())
            {
                string trackName = string.Empty;
                int channel = -1;
                int programNumber = 0;
                long maxTick = 0;

                foreach (var evt in track.Events)
                {
                    switch (evt)
                    {
                        case SequenceTrackNameEvent nameEvt:
                            trackName = nameEvt.Text ?? string.Empty;
                            break;
                        case ProgramChangeEvent pc:
                            programNumber = pc.ProgramNumber;
                            channel = pc.Channel;
                            break;
                        case NoteOnEvent noteOn:
                            if (channel == -1) channel = noteOn.Channel;
                            break;
                    }
                }

                // Use GetNotes() for accurate note count and length
                var notes = track.GetNotes().ToList();
                int noteCount = notes.Count;
                foreach (var n in notes)
                {
                    if (n.EndTime > maxTick) maxTick = n.EndTime;
                    if (channel == -1) channel = n.Channel;
                }

                long ppq = _midiFile!.TimeDivision is TicksPerQuarterNoteTimeDivision tpq
                    ? tpq.TicksPerQuarterNote : 480;

                int barCount = maxTick > 0
                    ? (int)Math.Ceiling(maxTick / (double)(ppq * 4))
                    : 0;

                bool isDrum = channel == 9;
                string instrName = isDrum ? "Drum Kit" :
                    (programNumber >= 0 && programNumber < GmInstruments.Length
                        ? GmInstruments[programNumber] : "Unknown");

                infos.Add(new MidiTrackInfo
                {
                    TrackIndex = idx,
                    Name = trackName,
                    Channel = channel,
                    NoteCount = noteCount,
                    IsDrumTrack = isDrum,
                    ProgramNumber = programNumber,
                    InstrumentName = instrName,
                    TotalTicks = maxTick,
                    BarCount = barCount
                });
                idx++;
            }

            // Only filter out tracks that are completely empty (no events at all)
            return infos.Where(t => t.NoteCount > 0 || t.BarCount > 0).ToList();
        }

        /// <summary>
        /// Extract notes from a track starting at barStart for stepCount 16th-note steps.
        /// Returns one StepModel per step (16 or 32).
        /// Each note gets quantised to the nearest step.
        /// </summary>
        public List<StepModel> ExtractSteps(int trackIndex, int barStart, int stepCount)
        {
            var steps = Enumerable.Range(0, stepCount)
                .Select(i => new StepModel(i))
                .ToList();

            if (_midiFile == null) return steps;

            long ppq = _midiFile.TimeDivision is TicksPerQuarterNoteTimeDivision tpq2
                ? tpq2.TicksPerQuarterNote : 480;

            long ticksPerStep = ppq / 4;  // 16th note
            long startTick = barStart * ppq * 4;
            long endTick = startTick + stepCount * ticksPerStep;

            var tracks = _midiFile.Chunks.OfType<TrackChunk>().ToList();
            if (trackIndex >= tracks.Count) return steps;

            var notes = tracks[trackIndex].GetNotes()
                .Where(n => n.Time >= startTick && n.Time < endTick)
                .OrderBy(n => n.Time)
                .ToList();

            foreach (var note in notes)
            {
                long relTick = note.Time - startTick;
                int stepIdx = (int)(relTick / ticksPerStep);
                if (stepIdx < 0 || stepIdx >= stepCount) continue;

                var step = steps[stepIdx];
                // If multiple notes hit the same step, keep the loudest
                if (!step.IsActive || note.Velocity > step.Velocity)
                {
                    step.IsActive = true;
                    step.NoteNumber = note.NoteNumber;
                    step.Velocity = note.Velocity;
                    step.LengthTicks = Math.Max(0.25, note.Length / (double)ticksPerStep);
                }
            }

            return steps;
        }

        /// <summary>Save a step sequence back out as a single-track MIDI file.</summary>
        public void ExportSteps(List<StepModel> steps, string outputPath,
            int channel = 0, int bpm = 120)
        {
            long ppq = 480;
            long ticksPerStep = ppq / 4;

            // Clamp channel to valid MIDI range 0-15 (-1 means unknown, default to 0)
            int safeChannel = Math.Clamp(channel < 0 ? 0 : channel, 0, 15);

            var track = new TrackChunk();
            using var manager = track.ManageNotes();

            foreach (var step in steps.Where(s => s.IsActive))
            {
                long startTick = step.StepIndex * ticksPerStep;
                long lenTicks = Math.Max(1, (long)(step.LengthTicks * ticksPerStep));

                manager.Objects.Add(new Note((SevenBitNumber)step.NoteNumber,
                    lenTicks, startTick)
                {
                    Channel = (FourBitNumber)safeChannel,
                    Velocity = (SevenBitNumber)Math.Clamp(step.Velocity, 1, 127),
                    OffVelocity = (SevenBitNumber)64
                });
            }

            // Set tempo
            var tempoTrack = new TrackChunk();
            tempoTrack.Events.Add(new SetTempoEvent((long)(60_000_000.0 / bpm)));

            var file = new MidiFile(tempoTrack, track)
            {
                TimeDivision = new TicksPerQuarterNoteTimeDivision((short)ppq)
            };

            file.Write(outputPath, overwriteFile: true);
        }
    }
}
