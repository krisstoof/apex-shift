# Options menu foundation

This document describes the Batch B settings implementation for Apex Shift.

## Scope

Implemented issues:

- #55 settings foundation
- #56 audio options and routing foundation
- #57 graphics options foundation

## Runtime files

```text
Assets/_Project/Scripts/Runtime/Settings/GameSettingsData.cs
Assets/_Project/Scripts/Runtime/Settings/GameSettingsService.cs
Assets/_Project/Scripts/Runtime/Settings/GameSettingsBootstrap.cs
Assets/_Project/Scripts/Runtime/Settings/AudioSettingsRuntime.cs
Assets/_Project/Scripts/Presentation/HUD/OptionsMenuController.cs
```

## Persistence

Settings are serialized as JSON into `PlayerPrefs` under:

```text
apex_shift.game_settings.v1
```

The service loads settings at startup, sanitizes unsafe values, applies settings, and saves after Apply/Reset Defaults.

## Audio

Available controls:

- Master
- Music
- Ambient
- SFX
- UI
- Mute

`AudioListener.volume` is used as the safe global fallback. Optional per-source routing is supported through `AudioSettingsRuntime`:

```csharp
AudioSettingsRuntime runtime = audioSourceGameObject.AddComponent<AudioSettingsRuntime>();
runtime.SetChannel(AudioChannel.Sfx);
```

Optional `AudioMixer` parameters are also supported if assigned on the component:

- `MasterVolume`
- `MusicVolume`
- `AmbientVolume`
- `SfxVolume`
- `UiVolume`

Missing mixer parameters are ignored safely.

## Graphics

Available controls:

- Fullscreen
- Resolution
- Quality preset
- VSync
- Target FPS
- Shadows
- Render Scale

Applied through:

- `Screen.SetResolution`
- `QualitySettings.SetQualityLevel`
- `QualitySettings.vSyncCount`
- `Application.targetFrameRate`
- `QualitySettings.shadows`
- optional URP render scale when the URP compile symbol is available

## UI integration

`GameSettingsBootstrap` is installed with `RuntimeInitializeOnLoadMethod`. It creates the settings service and binds an `OptionsMenuController` to the runtime-created `OptionsMenu` panel, including inactive menu objects.

This keeps the existing runtime HUD/menu construction intact while replacing the placeholder Options page with real controls.

## Manual test

1. Open Options from the start menu.
2. Change Master volume and click Apply.
3. Close and reopen Options; value should persist.
4. Restart Play Mode; value should persist.
5. Change fullscreen/windowed and resolution, click Apply.
6. Change quality preset, VSync and target FPS, click Apply.
7. Use Reset Defaults and verify values are restored.

## Known limitation

Dropdowns are runtime-generated with Unity UI primitives. The foundation is functional and extendable, but final visual styling can be improved later with authored prefabs.
