# Bass Note Finder

## Quick Start

1. Extract all files from this zip to a folder.
2. Run `BassNoteFinder.exe`.
3. In the footer, choose your microphone under `Input`.
4. Click `Start Mic`.
5. Choose either **Teacher Mode** or **Student Mode**.

## Basic Use

- **Teacher Mode**
  - Pick a note on the staff (or click `Random`).
  - Play that note on bass.

- **Student Mode**
  - Choose Random, Gentle, Adaptive, or Shuffle in the Coach dropdown.
  - On correct note detection, it can auto-advance.

Useful controls:
- `Gate`: raises/lowers noise threshold.
- `Bass Harmonics`: helps reduce octave-jump false positives.
- `Deviation`: shows cents offset in detected note display.

## Add a language

1. Open the `Languages` folder next to `BassNoteFinder.exe`.
2. Copy `translation-template.json` to a new file in that folder, such as `nl.json`.
3. Fill in `name` with the label you want to see in the picker, such as `Nederlands`. Translate the values under `strings` and keep the keys and placeholders such as `{0}` and `{1}`.
4. Save as UTF-8, restart Bass Note Finder, and select the pack under **Language** in the footer.

You can omit entries or leave their values blank to use the English text. Keep the filename stable so the app remembers your selection.

## Disclaimer

This software is provided "as is" without warranties of any kind.
Pitch detection accuracy and microphone behavior can vary by instrument,
audio interface, driver, environment, and system configuration.
Use at your own risk.
