# MIDI Step Extractor

A Windows WPF desktop application for extracting 16 or 32-step sequences from multi-track MIDI files, auditioning them through FluidSynth, and exporting them as single-track MIDI files ready for use in a sequencer.

---

## Features

| Feature | Details |
|---------|---------|
| **MIDI Import** | Load any standard .mid / .midi file with multiple tracks |
| **Track Selection** | Pick individual tracks — drums, bass, melody, etc. |
| **Step Grid** | Visual 16 or 32-step sequencer grid |
| **Per-Step Controls** | Toggle on/off · Note name · Velocity slider · Note length |
| **Playback** | Live playback via FluidSynth with GM SoundFont |
| **Export** | Save the sequence as a clean single-track MIDI file |

---

## Prerequisites

### 1. .NET 8 SDK
Download from: https://dotnet.microsoft.com/download/dotnet/8.0

### 2. FluidSynth Runtime (Required for audio playback)
Download FluidSynth 2.3.x for Windows:
- https://github.com/FluidSynth/fluidsynth/releases
- Download the `.zip` release for Windows (x64)
- Extract and copy `fluidsynth.dll` and `libfluidsynth-3.dll` into the app's output folder:
  `bin\Debug\net8.0-windows\` or next to the `.exe`

Alternatively install via **vcpkg**:
```
vcpkg install fluidsynth:x64-windows
```

### 3. A SoundFont (.sf2) File
FluidSynth needs a SoundFont to make sound. Recommended free options:

**GeneralUser GS** (recommended, ~31MB, excellent quality)
- https://schristiancollins.com/generaluser.php
- Download "GeneralUser GS v1.471.sf2"

**FluidR3_GM** (larger, ~140MB, very complete)
- Available from many Linux package mirrors
- Search: `FluidR3_GM.sf2 download`

**MuseScore General** (good drum kit)
- https://musescore.org/en/handbook/3/soundfonts-and-sfz-files

> **Using the app**: Click "Browse .sf2 File" in the SoundFont panel and select your downloaded .sf2 file.

---

## Building

```bash
# Clone / open the project folder
cd MidiStepExtractor

# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

Or open `MidiStepExtractor.csproj` in **Visual Studio 2022** (Community edition is free) and press F5.

---

## Usage

1. **Open MIDI** — Click "Open MIDI File" and select a `.mid` file
2. **Select Track** — Use the track dropdown; drum tracks are auto-detected (Channel 10)
3. **Set Start Bar** — Type the bar number to start from (0-indexed)
4. **Choose 16 or 32 Steps** — Click the step count buttons
5. **Load SoundFont** — Click "Browse .sf2 File" if you haven't already
6. **Play** — Click "▶ PLAY" to audition the sequence (loops by default)
7. **Edit Steps** — Click any step button to toggle it on/off; drag velocity sliders; change note length
8. **Export** — Click "⬇ Export as MIDI" to save the sequence as a `.mid` file

---

## Architecture

```
MidiStepExtractor/
├── Models/
│   ├── StepModel.cs          — Single step (note, velocity, length, active state)
│   └── MidiTrackInfo.cs      — Track summary from loaded MIDI file
├── ViewModels/
│   └── MainViewModel.cs      — MVVM ViewModel (CommunityToolkit.Mvvm)
├── Views/
│   └── MainWindow.xaml(.cs)  — Main WPF window
├── Services/
│   ├── MidiImportService.cs  — MIDI parsing & step extraction (DryWetMidi)
│   └── FluidSynthPlaybackService.cs — FluidSynth audio playback
├── Converters/
│   └── Converters.cs         — WPF value converters
└── Themes/
    └── DarkTheme.xaml        — Dark studio colour theme
```

---

## NuGet Packages Used

| Package | Purpose |
|---------|---------|
| `Melanchall.DryWetMidi` | MIDI file parsing and writing |
| `NFluidsynth` | .NET binding for FluidSynth audio |
| `NAudio` | Audio fallback (MIDI output) |
| `CommunityToolkit.Mvvm` | Source-generated MVVM (ObservableProperty etc.) |

---

## Next Steps: Building the Sequencer

The exported MIDI file from this tool is designed to plug directly into a single-track sequencer. Suggested next features:

- Load the exported `.mid` into a step sequencer UI
- Add swing/shuffle controls
- Add per-step probability and ratchet
- Connect to a hardware MIDI output via NAudio
- Add pattern chaining (A → B → C)

---

## Troubleshooting

| Problem | Solution |
|---------|---------|
| No sound on playback | Check FluidSynth DLLs are in the output folder; ensure .sf2 is loaded |
| "Failed to load SoundFont" | Make sure the .sf2 path has no special characters; try a different SF2 |
| Playback starts then stops | Install the FluidSynth runtime DLLs (see Prerequisites) |
| Wrong notes for drum track | Confirm the track is on Channel 10 (shown as "Drum Kit" in instrument name) |
| Build errors about NFluidsynth | Ensure FluidSynth 2.3.x native DLLs are present at runtime |
