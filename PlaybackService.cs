using NAudio.Midi;
using MidiStepExtractor.Models;

namespace MidiStepExtractor.Services
{
    /// <summary>
    /// Plays step sequences using NAudio's MidiOut (Windows built-in MIDI synth).
    /// No external DLLs or SoundFont required for basic playback.
    /// 
    /// For higher-quality sound with a SoundFont (.sf2), install:
    /// VirtualMIDISynth (free): https://coolsoft.altervista.org/en/virtualmidisynth
    /// Then load your .sf2 in VirtualMIDISynth and select it as device index 1+.
    /// </summary>
    public class PlaybackService : IDisposable
    {
        private MidiOut? _midiOut;
        private bool _disposed;
        private CancellationTokenSource? _playCts;

        public bool IsInitialised => _midiOut != null;
        public bool IsPlaying => _playCts != null && !_playCts.IsCancellationRequested;

        public event Action<int>? StepTriggered;
        public event Action? PlaybackStopped;

        public static List<string> GetOutputDevices()
        {
            var devices = new List<string>();
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
                devices.Add(MidiOut.DeviceInfo(i).ProductName);
            return devices;
        }

        public bool Initialise(int deviceIndex = 0)
        {
            DisposeDevice();
            if (MidiOut.NumberOfDevices == 0)
                throw new InvalidOperationException(
                    "No MIDI output devices found.\n\n" +
                    "Install VirtualMIDISynth for SoundFont support:\n" +
                    "https://coolsoft.altervista.org/en/virtualmidisynth");

            int idx = Math.Clamp(deviceIndex, 0, MidiOut.NumberOfDevices - 1);
            _midiOut = new MidiOut(idx);
            return true;
        }

        public void PlaySequence(List<StepModel> steps, int bpm, int channel,
            int programNumber, bool loop = false)
        {
            if (_midiOut == null) Initialise();
            if (_midiOut == null) return;

            StopPlayback();
            _playCts = new CancellationTokenSource();
            var token = _playCts.Token;

            if (channel != 9)
                _midiOut.Send(MidiMessage.ChangePatch(programNumber, channel).RawData);

            double msPerStep = 60_000.0 / bpm / 4.0;

            Task.Run(async () =>
            {
                try
                {
                    do
                    {
                        foreach (var step in steps)
                        {
                            token.ThrowIfCancellationRequested();
                            StepTriggered?.Invoke(step.StepIndex);

                            if (step.IsActive && _midiOut != null)
                            {
                                int vel  = Math.Clamp(step.Velocity, 1, 127);
                                int note = Math.Clamp(step.NoteNumber, 0, 127);

                                _midiOut.Send(MidiMessage.StartNote(note, vel, channel).RawData);

                                double noteDurMs = msPerStep * Math.Clamp(step.LengthTicks, 0.1, 3.9);
                                int capturedNote = note; int capturedCh = channel;
                                _ = Task.Delay((int)noteDurMs, token)
                                    .ContinueWith(t =>
                                    {
                                        if (!t.IsCanceled)
                                            _midiOut?.Send(MidiMessage.StopNote(capturedNote, 0, capturedCh).RawData);
                                    }, TaskScheduler.Default);
                            }

                            await Task.Delay((int)msPerStep, token);
                        }
                    } while (loop && !token.IsCancellationRequested);
                }
                catch (OperationCanceledException) { }
                finally
                {
                    AllNotesOff();
                    PlaybackStopped?.Invoke();
                }
            }, token);
        }

        public void StopPlayback()
        {
            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = null;
            AllNotesOff();
        }

        private void AllNotesOff()
        {
            if (_midiOut == null) return;
            try
            {
                for (int ch = 0; ch < 16; ch++)
                    _midiOut.Send(new ControlChangeEvent(0, ch + 1,
                        MidiController.AllNotesOff, 0).GetAsShortMessage());
            }
            catch { /* ignore if device closed */ }
        }

        private void DisposeDevice()
        {
            StopPlayback();
            _midiOut?.Dispose();
            _midiOut = null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            DisposeDevice();
        }
    }
}
