# Bass Note Finder

## Quick Start

1. Extract all files from this zip to a folder.
2. Run `BassNoteFinder.exe`. If you see a .NET download prompt, install the **.NET Desktop Runtime 10.x (Windows x64)** and then open the app again. You can also download it directly from [Microsoft’s .NET 10 downloads](https://dotnet.microsoft.com/download/dotnet/10.0).
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

Choose English, Greek, or Dutch from the **Language** picker in the footer. To create another translation, use the template in the `Languages` folder and follow the [language pack guide](docs/language-packs.md).

## Third-party licenses

The package includes license texts for NAudio and the .NET launcher. See [third-party notices](THIRD-PARTY-NOTICES.txt).

## Disclaimer

This software is provided "as is" without warranties of any kind.
Pitch detection accuracy and microphone behavior can vary by instrument,
audio interface, driver, environment, and system configuration.
Use at your own risk.
