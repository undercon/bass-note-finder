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
- No .NET runtime install required (self-contained release).

## Using the Release Package

1. Download `BassNoteFinder-<tag>-win-x64.zip` from GitHub Releases.
2. Extract the zip to a folder you can write to (for app settings persistence).
3. Run `BassNoteFinder.exe`.
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

## Controls at a Glance

- `Input`: choose microphone device.
- `Gate`: input threshold to ignore low-level noise.
- `Bass Harmonics`: reduces octave-jump false positives.
- `Start/Stop Mic`: toggles capture; startup state is persisted.
- `Detected`: latest stable note (optional cents deviation).

Mode-specific controls include note name display, accidental toggle, octave display, random target selection, and mode navigation.

## Release Automation (GitHub Actions)

Two workflows are included:

- `.github/workflows/ci.yml`
  - Runs on push/PR.
  - Restores, builds app, and runs tests.

- `.github/workflows/release.yml`
  - Runs on tag push.
  - Restores and builds **app project only** in `Release` configuration.
  - Publishes self-contained `win-x64` app, zips output, and creates/updates a GitHub Release.
  - Excludes test project packaging and test execution (tests are CI-only).
  - Excludes PDB symbols from release output.
  - Includes this `README.md` in the release zip.
  - Applies automated build-number versioning using GitHub Actions run number.

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

The workflow will attach an artifact named like:

`BassNoteFinder-<tag>-win-x64.zip`

## Packaging Notes

- Only the app project is published (`BassNoteFinder/BassNoteFinder.csproj`), so test binaries are not included in release artifacts.
- Release publish settings use standard `dotnet publish` options and generate a self-contained single-file executable for Windows x64.
- `README.md` is copied into the packaged artifact from repo root as the single source of truth.

## Disclaimer

This software is provided "as is", without warranty of any kind, express or implied.
Use it at your own risk. Audio input behavior, pitch detection accuracy, and device compatibility can vary by system and hardware.

## Cross-Platform Roadmap Note

The current UI stack is WPF (`net10.0-windows`), so shipped binaries target Windows today.
The release workflow is intentionally simple and can be extended later with additional runtimes when/if a cross-platform UI is introduced.
