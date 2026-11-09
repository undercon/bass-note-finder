# Bass Note Finder

A desktop trainer for bass note recognition and fretboard mapping.

Current app modes:
- Teacher Mode: you choose the note on staff, then play it.
- Student Mode: app assigns random notes and advances drills automatically.

## Requirements

### Run from source
- Windows 10/11
- .NET SDK 10.x

### Run from release package
- Windows 10/11 x64
- Microsoft .NET Desktop Runtime 10.x (Windows x64).

## Using the Release Package

1. Download `BassNoteFinder-<tag>-win-x64-framework-dependent.zip` from GitHub Releases.
2. Extract the zip to a folder you can write to (for app settings persistence).
3. Run `BassNoteFinder.exe`. If Windows shows a .NET download prompt, install the **.NET Desktop Runtime 10.x (Windows x64)** and then open the app again. [Microsoft .NET 10 downloads](https://dotnet.microsoft.com/download/dotnet/10.0)
4. Choose your input device, then click `Start Mic`.

First-run defaults:
- Mic starts off.
- Harmonic correction starts on.
- Input threshold defaults to `0.010`.

## Local Development

From repo root:

```powershell
dotnet restore
dotnet build BassNoteFinder/BassNoteFinder.csproj
dotnet test BassNoteFinder.Tests/BassNoteFinder.Tests.csproj
dotnet run --project BassNoteFinder/BassNoteFinder.csproj
```

The default test run excludes pitch-fixture tests tagged `Category=Pitch`. Run them explicitly when changing pitch detection:

```powershell
dotnet test BassNoteFinder.Tests/BassNoteFinder.Tests.csproj --settings BassNoteFinder.Tests/all.runsettings --filter "Category=Pitch"
```

### Visual Review Harness

Generate deterministic screenshots for AI or human UI review:

```powershell
dotnet run --project VisualReviewHarness/VisualReviewHarness.csproj -- --mode all --output .artifacts/visual-review
```

The harness launches the real WPF shell with mic/config side effects disabled, captures Menu/Teacher/Student screenshots, and writes `AI_REVIEW_PROMPT.md` next to the images.

## Controls at a Glance

- `Input`: choose microphone device.
- `Gate`: input threshold to ignore low-level noise.
- `Bass Harmonics`: reduces octave-jump false positives.
- `Start/Stop Mic`: toggles capture; startup state is persisted.
- `Detected`: latest stable note (optional cents deviation).

Mode-specific controls include note label display, accidental targets, octave matching, random target selection, and mode navigation.
Student Mode also includes an available-note selector, auto-next delay controls, and a Coach picker with Random, Gentle, Adaptive, and Shuffle.

Most settings are remembered between launches, including footer controls, Teacher/Student mode options, Student available notes, coach preference, auto-next delay, and window size/position. Practice history is session-based.

The footer theme picker supports Dark, Light, and System. System follows the operating-system color palette exposed by the UI framework.
The notation picker switches displayed note names between Standard (C, C♯, B♭) and Solfège (Do, Do♯, Si♭).
The language picker includes English, Greek, Dutch, and any custom JSON packs in the `Languages` folder next to the executable. See [Create a language pack](docs/language-packs.md) for the format and steps.

## Release Automation (GitHub Actions)

Two workflows are included:

- `.github/workflows/ci.yml`
  - Runs on push/PR.
  - Restores, builds app, and runs tests.

- `.github/workflows/release.yml`
  - Runs on tag push.
  - Uses `scripts/package-windows-release.ps1` to publish and package the app as a framework-dependent Windows x64 build.
  - Attaches the ZIP to a GitHub Release.
  - Excludes test project packaging and test execution (tests are CI-only).
  - Excludes PDB symbols from release output.
  - Includes the quick-start guide and third-party notices in the ZIP.
  - Stamps release builds from the pushed tag value (supports `v` prefix).

## Versioning

- Local default app version is `0.1.0-local`.
- CI workflow computes:
  - `APP_VERSION = 0.1.<run_number>`
  - `APP_INFO_VERSION = 0.1.<run_number>+sha.<short_sha>`
- Release workflow uses the pushed tag as app version (for example, tag `0.3.3` -> app shows `v0.3.3`).
- The app displays the current version on the mode selection screen (top-right badge).

### Trigger a release

```powershell
git tag 0.2
git push origin 0.2
```

The workflow attaches `BassNoteFinder-<tag>-win-x64-framework-dependent.zip`.

## Packaging Notes

- Only the app project is published (`BassNoteFinder/BassNoteFinder.csproj`), so test binaries are not included in release artifacts.
- The ZIP contains a framework-dependent single-file executable for Windows x64 and its companion files.
- The package includes `RELEASE_README.md` as `README.md`, plus the [third-party notices](THIRD-PARTY-NOTICES.txt) and license texts for NAudio and the .NET executable host.

## Disclaimer

This software is provided "as is", without warranty of any kind, express or implied.
Use it at your own risk. Audio input behavior, pitch detection accuracy, and device compatibility can vary by system and hardware.

## Cross-Platform Roadmap Note

The current UI stack is WPF (`net10.0-windows`), so shipped binaries target Windows today.
The release workflow is intentionally simple and can be extended later with additional runtimes when/if a cross-platform UI is introduced.
