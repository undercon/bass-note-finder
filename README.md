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
  - Restores, builds, runs tests, publishes self-contained `win-x64` app, zips output, and creates/updates a GitHub Release.
  - Applies automated build-number versioning using GitHub Actions run number.

## Versioning

- Local default app version is `0.1.0-local`.
- CI/Release workflows compute:
  - `APP_VERSION = 0.1.<run_number>`
  - `APP_INFO_VERSION = 0.1.<run_number>+sha.<short_sha>`
- The app displays the current version on the mode selection screen (top-right badge).

### Trigger a release

```powershell
git tag 0.1_alpha
git push origin 0.1_alpha
```

The workflow will attach an artifact named like:

`BassNoteFinder-<tag>-win-x64.zip`

## Packaging Notes

- Only the app project is published (`BassNoteFinder/BassNoteFinder.csproj`), so test binaries are not included in release artifacts.
- Release publish settings use standard `dotnet publish` options and generate a self-contained single-file executable for Windows x64.

## Cross-Platform Roadmap Note

The current UI stack is WPF (`net10.0-windows`), so shipped binaries target Windows today.
The release workflow is intentionally simple and can be extended later with additional runtimes when/if a cross-platform UI is introduced.
