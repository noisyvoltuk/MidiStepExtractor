using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melanchall.DryWetMidi.Core;
using MidiStepExtractor.Models;
using MidiStepExtractor.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace MidiStepExtractor.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly MidiImportService _midiService = new();
        private readonly PlaybackService _playback = new();

        // ── File & Track state ────────────────────────────────────────────
        [ObservableProperty] private string _midiFilePath = string.Empty;
        [ObservableProperty] private string _statusMessage = "Open a MIDI file to begin.";
        [ObservableProperty] private bool _isMidiLoaded;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasTrackSelected))]
        private MidiTrackInfo? _selectedTrack;

        public ObservableCollection<MidiTrackInfo> Tracks { get; } = [];

        // ── Step grid state ───────────────────────────────────────────────
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StepCountLabel))]
        private int _stepCount = 16;

        [ObservableProperty] private int _startBar = 0;
        [ObservableProperty] private int _maxBar = 0;

        public ObservableCollection<StepModel> Steps { get; } = [];

        // ── Playback state ────────────────────────────────────────────────
        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private bool _loopPlayback = true;
        [ObservableProperty] private int _bpm = 120;
        [ObservableProperty] private int _playheadStep = -1;

        // ── MIDI device selection ─────────────────────────────────────────
        [ObservableProperty] private int _selectedDeviceIndex = 0;
        public ObservableCollection<string> MidiDevices { get; } = [];

        // ── Export state ──────────────────────────────────────────────────
        [ObservableProperty] private bool _hasSteps;

        public bool HasTrackSelected => SelectedTrack != null;
        public string StepCountLabel => $"{StepCount} Steps";

        // ─────────────────────────────────────────────────────────────────

        public MainViewModel()
        {
            _playback.StepTriggered += OnStepTriggered;
            _playback.PlaybackStopped += OnPlaybackStopped;
            RefreshMidiDevices();
        }

        // ── Device management ─────────────────────────────────────────────

        [RelayCommand]
        private void RefreshMidiDevices()
        {
            MidiDevices.Clear();
            var devices = PlaybackService.GetOutputDevices();
            if (devices.Count == 0)
            {
                MidiDevices.Add("No MIDI devices found");
                StatusMessage = "No MIDI output devices. Install VirtualMIDISynth for SoundFont support.";
            }
            else
            {
                foreach (var d in devices) MidiDevices.Add(d);
                SelectedDeviceIndex = 0;
                StatusMessage = $"Found {devices.Count} MIDI output device(s). Open a MIDI file to begin.";
            }
        }

        // ── File Commands ─────────────────────────────────────────────────

        [RelayCommand]
        private void OpenMidiFile()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Open MIDI File",
                Filter = "MIDI Files (*.mid;*.midi)|*.mid;*.midi|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var tracks = _midiService.LoadFile(dlg.FileName);
                var allTracks = _midiService.LoadedFile?.Chunks.OfType<TrackChunk>().Count() ?? 0;
                MidiFilePath = dlg.FileName;
                Tracks.Clear();
                foreach (var t in tracks) Tracks.Add(t);

                SelectedTrack = Tracks.FirstOrDefault();
                IsMidiLoaded = Tracks.Count > 0;
                StatusMessage = $"Loaded: {Path.GetFileName(dlg.FileName)} — {allTracks} chunks, {tracks.Count} tracks with notes";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading MIDI: {ex.Message}";
                MessageBox.Show(ex.Message, "MIDI Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Step Extraction ───────────────────────────────────────────────

        [RelayCommand]
        private void ExtractSteps()
        {
            if (SelectedTrack == null) return;

            try
            {
                var extracted = _midiService.ExtractSteps(
                    SelectedTrack.TrackIndex, StartBar, StepCount);

                Steps.Clear();
                foreach (var s in extracted) Steps.Add(s);

                HasSteps = Steps.Any(s => s.IsActive);
                MaxBar = Math.Max(0, SelectedTrack.BarCount - 1);
                StatusMessage = $"Extracted {Steps.Count(s => s.IsActive)} active steps from bar {StartBar + 1}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Extraction error: {ex.Message}";
            }
        }

        [RelayCommand] private void SetStep16() { StepCount = 16; ExtractSteps(); }
        [RelayCommand] private void SetStep32() { StepCount = 32; ExtractSteps(); }

        // ── Playback ──────────────────────────────────────────────────────

        [RelayCommand]
        private void TogglePlayback()
        {
            if (IsPlaying)
            {
                _playback.StopPlayback();
                IsPlaying = false;
                PlayheadStep = -1;
                return;
            }

            if (!Steps.Any(s => s.IsActive))
            {
                StatusMessage = "No active steps to play. Extract steps first.";
                return;
            }

            try
            {
                _playback.Initialise(SelectedDeviceIndex);
                int rawChannel = SelectedTrack?.Channel ?? 0;
                int channel = SelectedTrack?.IsDrumTrack == true ? 9 : Math.Clamp(rawChannel < 0 ? 0 : rawChannel, 0, 15);
                int program = SelectedTrack?.ProgramNumber ?? 0;
                _playback.PlaySequence(Steps.ToList(), Bpm, channel, program, LoopPlayback);
                IsPlaying = true;
                StatusMessage = $"Playing {StepCount} steps at {Bpm} BPM via {MidiDevices.ElementAtOrDefault(SelectedDeviceIndex) ?? "MIDI Out"}...";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Playback error: {ex.Message}";
                MessageBox.Show(ex.Message + "\n\nFor SoundFont quality, install VirtualMIDISynth:\nhttps://coolsoft.altervista.org/en/virtualmidisynth",
                    "Playback Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void StopPlayback()
        {
            _playback.StopPlayback();
            IsPlaying = false;
            PlayheadStep = -1;
        }

        private void OnStepTriggered(int stepIndex)
        {
            Application.Current?.Dispatcher.InvokeAsync(() => PlayheadStep = stepIndex);
        }

        private void OnPlaybackStopped()
        {
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                IsPlaying = false;
                PlayheadStep = -1;
                if (!LoopPlayback) StatusMessage = "Playback finished.";
            });
        }

        // ── Export ────────────────────────────────────────────────────────

        [RelayCommand]
        private void ExportMidi()
        {
            if (!Steps.Any(s => s.IsActive))
            {
                StatusMessage = "No active steps to export.";
                return;
            }

            string suggestedName = Path.GetFileNameWithoutExtension(MidiFilePath) +
                $"_{SelectedTrack?.Name ?? "track"}_bar{StartBar + 1}_{StepCount}steps.mid";

            var dlg = new SaveFileDialog
            {
                Title = "Export Step Sequence as MIDI",
                Filter = "MIDI Files (*.mid)|*.mid",
                FileName = suggestedName
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                int rawChannel = SelectedTrack?.Channel ?? 0;
                int channel = SelectedTrack?.IsDrumTrack == true ? 9 : Math.Clamp(rawChannel < 0 ? 0 : rawChannel, 0, 15);
                _midiService.ExportSteps(Steps.ToList(), dlg.FileName, channel, Bpm);
                StatusMessage = $"Exported: {Path.GetFileName(dlg.FileName)}";
                MessageBox.Show($"Sequence exported!\n{dlg.FileName}",
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export error: {ex.Message}";
                MessageBox.Show(ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Step editing ──────────────────────────────────────────────────

        public void ToggleStep(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= Steps.Count) return;
            Steps[stepIndex].IsActive = !Steps[stepIndex].IsActive;
            HasSteps = Steps.Any(s => s.IsActive);
        }

        // ──────────────────────────────────────────────────────────────────

        partial void OnSelectedTrackChanged(MidiTrackInfo? value)
        {
            if (value != null)
            {
                MaxBar = Math.Max(0, value.BarCount - 1);
                StartBar = 0;
                ExtractSteps();
            }
        }

        partial void OnStartBarChanged(int value) => ExtractSteps();
        partial void OnBpmChanged(int value)
        {
            if (IsPlaying) { StopPlayback(); TogglePlayback(); }
        }

        public void Dispose() => _playback.Dispose();
    }
}
