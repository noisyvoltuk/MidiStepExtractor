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


---

## Usage

1. **Open MIDI** — Click "Open MIDI File" and select a `.mid` file
2. **Select Track** — Use the track dropdown; drum tracks are auto-detected (Channel 10)
3. **Set Start Bar** — Type the bar number to start from (0-indexed)
4. **Choose 16 or 32 Steps** — Click the step count buttons
5. **Select output** 
6. **Play** — Click "▶ PLAY" to audition the sequence (loops by default)
7. **Edit Steps** — Click any step button to toggle it on/off; drag velocity sliders; change note length
8. **Export** — Click "⬇ Export as MIDI" to save the sequence as a `.mid` file

---

## Architecture


