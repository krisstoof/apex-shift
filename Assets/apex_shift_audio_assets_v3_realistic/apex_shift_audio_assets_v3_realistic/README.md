# Apex Shift — Audio Assets v3 realistic-ish

More natural procedural foley-style WAV pack.

## Important

These are still procedurally generated, not recorded samples. They are more realistic than v2 because they use layered broadband transients, less musical/arcade tones, short room reflections, more natural attack/decay curves and noise-based material textures.

For final commercial polish, replace the most important hero sounds with real foley recordings or licensed libraries.

## Import

Copy into Unity so the final path is:

`Assets/_Project/Audio`

## Suggested #44 clips

- melee swing: `SFX/combat/player/spear_swing_real_*.wav`
- melee hit: `SFX/combat/player/spear_hit_flesh_real_*.wav`
- bow release: `SFX/combat/player/bow_release_real_*.wav`
- trap trigger: `SFX/combat/traps/trap_snap_real_*.wav`
- Varnak attack/hurt/death: `SFX/creatures/varnak/*real*.wav`

## Unity import

- SFX: mono, Compressed In Memory or Decompress On Load.
- Ambience loops: stereo, Loop enabled.
- Start mix:
  - Combat: -6 dB
  - Creature: -7 dB
  - UI: -12 dB
  - Ambience: -22 dB
