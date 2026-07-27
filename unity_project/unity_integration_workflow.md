# Unity integration workflow

## Working project

Editable Unity project:

`C:\Users\diego\Unity_MasterDegree_Project\UNIFESP-Project-main`

Reference/decompiled build:

`C:\Users\diego\OneDrive\Documents\GitHub\MasterDegree\unity_project`

The editable project should be treated as the source of truth. The decompiled build is useful for comparison and recovery only.

## Current project facts

- Unity version: `6000.4.4f1`.
- LSL package: `com.labstreaminglayer.lsl4unity`.
- Unity MCP package is already present: `com.coplaydev.unity-mcp`.
- Main build scenes:
  - `Assets/Scenes/LoadingScene.unity`
  - `Assets/Scenes/Gameplay.unity`
  - `Assets/Scenes/Menu.unity`
  - `Assets/Scenes/Experimento Um.unity`
  - `Assets/Scenes/Experimento Dois.unity`
  - `Assets/Scenes/Game UI.unity`
  - `Assets/Scenes/Game UI 2.unity`

## Key scripts

- `Assets/Scripts/LSL/LSLReceiver.cs`
  - Receives `Python_EEG`.
  - Pulls samples every frame.
  - Sends `data[0]`, `data[1]`, `data[2]` to `PlayerController.UpdateLegValues`.
  - Has an offline bypass when the stream is not found.

- `Assets/Scripts/Managers/UIConfigs.cs`
  - Owns experiment configuration UI.
  - Starts LSL initialization.
  - Applies Graz timing values.
  - Starts realtime logging.

- `Assets/Scripts/Graz/GrazController.cs`
  - Runs baseline, fixation, cue, feedback, and pause phases.
  - Drives cue image, ball launch, feedback phase state, and marker fields in `RealtimeLog`.

- `Assets/Scripts/Managers/StartUm.cs`
  - Starts experiment 1 after Space.
  - Starts logging and Graz protocol.

- `Assets/Scripts/Managers/StartDois.cs`
  - Starts experiment 2 after Space.
  - Enables the meter and starts logging/Graz.

- `Assets/Scripts/Camera/PlayerController.cs`
  - Controls prosthesis/leg visibility.
  - Applies leg input values from LSL.
  - Updates camera controls and realtime log values.

- `Assets/Scripts/Leg/MedidorMovement.cs`
  - Implements continuous feedback overlap detection.
  - Triggers success/error feedback.

- `Assets/Scripts/Log/RealtimeLog.cs`
  - Writes trial state and signal/feedback values.
  - Currently writes a formatted realtime file, then rewrites it as pipe-separated values on end.

## Initial architecture issues

1. LSL input is tightly coupled to scene objects and assumes at least three channels.
2. `streamType` is exposed in UI but not used for stream resolution.
3. There is no explicit connection state object for UI/status/logging.
4. Offline testing exists as a bypass, but not as a real simulation mode.
5. LSL marker output is present only as commented code.
6. Graz timing and logging markers are set directly inside the coroutine, making synchronization harder to audit.
7. Logging format changes between realtime writes and final writes.
8. Some UI flow depends on object names, singleton lookups, and Inspector wiring.

## Recommended iteration loop

1. Keep using Unity Editor for scene/prefab wiring and visual validation.
2. Use direct file edits for C# scripts and small prefab/scene YAML changes only when safe.
3. Use Unity MCP mainly for Editor inspection, Play Mode checks, console logs, and fast iteration.
4. Add a real simulation mode before changing UI heavily.
5. Refactor in small branches/steps:
   - LSL connection state and validation.
   - Simulation input provider.
   - Marker output.
   - UI connection/status panel.
   - Logging cleanup.
   - Feedback/UI redesign.

## First recommended code change

Create an input-provider layer:

- `IBciInputProvider`
- `LslBciInputProvider`
- `SimulatedBciInputProvider`
- A small controller that feeds `PlayerController` from either provider.

This lets us test UI, feedback, leg movement, and Graz flow without requiring the EEG/EMG pipeline to be running every time.
