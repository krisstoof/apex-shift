# Unity Camera Setup

The prototype uses a Cinemachine-based orthographic isometric camera.

## Main settings

- Projection: Orthographic
- Orthographic Size: 14
- Pitch: 35.264
- Yaw: 45
- Roll: 0

## Generated scene hierarchy

- Main Camera
  - Camera
  - CinemachineBrain
- PlayerFollowCamera
  - Cinemachine camera component
  - Follow: Player

## Fallback

`IsometricCameraFollow` remains only as fallback and should not drive the main camera when Cinemachine is active.

## Movement compatibility

Player movement is camera-relative and uses `Camera.main`.

The generated Main Camera keeps the `MainCamera` tag.
