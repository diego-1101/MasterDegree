# Experiment 2 Unity/GRAZ redesign

## Goal

Turn the Unity project into the main experimental interface for Experiment 2, replacing the current split workflow where PsychoPy runs the GRAZ protocol while Unity runs the visual feedback separately.

The Unity application should support both operator-facing configuration and subject-facing stimulation/feedback, while keeping LSL input, LSL markers, trial randomization, and logs synchronized.

## Core principle

Unity should not be only a visualization layer. It should become the experimental orchestrator for:

- free visual exploration and screenshot capture;
- free LSL-driven control/familiarization;
- GRAZ acquisition for training data;
- GRAZ feedback/testing with classifier-driven prosthesis control.

Classes/cues should not be hardcoded. The operator should be able to create, edit, reuse, and save cue/class presets for each experiment design.

## Experiment 2 modes

### 1. Free capture mode

Purpose:

- Explore the environment freely.
- Control camera, view, prosthesis/leg pose, visibility, and scene composition.
- Capture screenshots for presentations, project documentation, papers, and future GRAZ cue images.
- Work as an experimenter-only tool. This mode is independent from the subject-facing GRAZ execution modes.

LSL:

- No LSL connection required.
- Optional simulated leg/camera controls are enough.

Subject view:

- Not necessarily used as an experiment mode.
- This mode is mainly for the researcher/operator.

Required controls:

- Camera position/view presets.
- Prosthesis/leg pose controls.
- Environment/visibility toggles.
- Screenshot button and output path.
- Numeric readouts for leg input and/or resulting angle.
- Numeric readouts for camera rotation, horizontal/vertical movement, and zoom.
- Optional fullscreen clean view with no UI.

Relationship with GRAZ cues:

- The capture mode is not required before running GRAZ.
- It only helps the experimenter produce cue image assets.
- GRAZ acquisition/feedback modes should let the operator choose existing cue image files for each class.
- The selected cue images are stored in the cue/class preset.
- The capture mode should save images to a stable folder so they can be picked later from the cue preset/configuration UI.

### 2. Free LSL control mode

Purpose:

- Let the subject interact with the virtual prosthesis before formal trials.
- Receive classification/angle input through LSL.
- Move the leg in real time from classifier output.
- Work as a familiarization/game-like mode.

LSL:

- Requires a classifier stream with two `float32` channels: discrete class code and probability.
- Does not publish GRAZ markers and does not create baseline, fixation, cue, task, rest, trial order, or class markers.

Subject view:

- Environment and prosthesis visible.
- The selected angle bars, lateral views, and ghost targets remain visible continuously until the operator presses `Esc`.
- The operator can show the right leg, left leg, or both.
- The green target/tolerance region is optional and can be hidden without hiding the angle bar or its red current-angle marker.

Operator controls:

- LSL stream name/type/source selection.
- Connection status.
- Editable mapping from class code to leg and target angle, with the built-in codes retained as fallback.
- Movement speed scaled by the received class probability.
- Configurable gravity/fall speed and stale-input timeout.
- Continuous visual feedback selection, per-side target, and tolerance.
- Start/stop session.

Logs:

- Session metadata.
- LSL connection details.
- Input values and leg angle over time.
- Optional manual annotations.

### 3. GRAZ acquisition mode

Purpose:

- Run a clean GRAZ-style acquisition protocol for EEG/EMG model training.
- Present fixation, cue, task, pause/rest, and break phases.
- Send reliable LSL markers.
- Save a complete local log of trial timing, class, session metadata, and protocol parameters.

LSL:

- Requires marker outlet.
- Does not require classifier input.
- May run alongside EEG/EMG acquisition streams handled outside Unity.

Subject view:

- Minimal, clean, fixed visual stimuli.
- The subject should see only what matters for the protocol.
- No game UI, no technical status, no distracting controls.

Operator controls:

- Subject ID.
- Session ID.
- Run number or number of runs.
- Number of repetitions per class.
- Selected cue/class preset.
- Editable class list.
- Class-to-marker mapping with automatic default codes and manual overrides.
- Cue image per class.
- Global cue size and color/tint for the subject screen.
- Description/notes per class.
- Fixation image.
- Fixation cross color and screen-relative size when no image is selected.
- Rest/pause image.
- Optional baseline, task, rest, success, and failure text with independent color and screen-relative size.
- Timing for baseline, fixation, cue, task, feedback if enabled, pause, and breaks.
- Randomization mode.
- Random seed.
- Preview/export of generated trial order.

Trial phases:

- `session_start`
- `run_start`
- `trial_start`
- `baseline`
- `fixation`
- `cue`
- `task`
- `pause`
- `trial_end`
- `run_end`
- `session_end`

Markers:

- Use a stable semantic marker schema.
- Also support numeric GRAZ-compatible codes when useful for downstream processing.
- For the active experiment, the LSL marker stream may transmit only the class/phase code when a minimal numeric stream is preferred.
- The local log must preserve the full metadata for each transmitted code.
- Trial order must be generated before the run starts so markers and logs agree.

Example semantic marker:

```text
cue_onset|trial=12|class=feet|marker=771|run=1|subject=S01|session=01
```

Example local log fields:

- timestamp_unix_ms
- lsl_local_clock
- subject_id
- session_id
- experiment_id
- mode
- run_index
- trial_index
- phase
- class_id
- class_label
- marker_code
- marker_label
- planned_phase_duration
- actual_phase_duration
- cue_image
- random_seed
- trial_order_index

### 4. GRAZ feedback/test mode

Purpose:

- Run a GRAZ-style trial while receiving classifier/angle input via LSL.
- Present cue, then give visual prosthesis feedback.
- Evaluate whether the subject reaches and holds the target angle.
- Log feedback behavior and performance.

LSL:

- Requires marker outlet.
- Requires classifier/angle input stream.

Subject view:

- Clean GRAZ cue/fixation presentation.
- During feedback/task, show prosthesis/leg and target region.
- Avoid exposing operator settings to the subject.

Operator controls:

- All acquisition controls from GRAZ acquisition mode.
- LSL classifier input settings.
- Target angle per class.
- Angle tolerance.
- Required hold time inside target.
- Trial timeout.
- Gravity/resistance.
- Input smoothing.
- Success/failure feedback settings.
- Optional hold-time progress feedback during task, red while accumulating and green when the required hold time is reached.
- Whether to continue after failure or repeat failed trials.

Feedback metrics:

- Target angle.
- Current angle.
- Signed error.
- Absolute error.
- Within target: true/false.
- Accumulated hold time.
- Success/failure.
- Time to first target entry.
- Time to success.
- Number of target exits.
- Class prediction/angle samples over time.

Additional log fields:

- target_angle
- angle_tolerance
- required_hold_time
- gravity
- current_angle
- input_value
- predicted_class
- classifier_confidence
- within_target
- accumulated_hold_time
- trial_result
- time_to_success
- target_exit_count

## Proposed architecture

### Data/configuration

- `CueClassDefinition`
  - Stable cue/class ID.
  - Human-readable label.
  - Numeric marker/class code.
  - Cue image.
  - Description.
  - Optional target angle.
  - Optional feedback parameters.
  - Enabled/disabled state.

- `CueClassPreset`
  - Reusable named set of `CueClassDefinition` entries.
  - Example: "Simple: one leg movement at different angles".
  - Stores default repetitions and optional default timing.
  - Can be duplicated and edited without losing previous presets.

- `ExperimentSessionConfig`
  - Subject/session/run metadata.
  - Selected mode.
  - Random seed.
  - Log output settings.
  - Selected cue/class preset.

- `GrazProtocolConfig`
  - Active classes from the selected preset.
  - Markers.
  - Timing.
  - Stimulus images.
  - Repetitions and randomization.

- `FeedbackProtocolConfig`
  - Target angles.
  - Tolerances.
  - Required hold times.
  - Gravity/resistance.
  - Timeout and success/failure behavior.

- `TrialDefinition`
  - One generated trial.
  - Class, marker, cue image, target angle, phase durations.
  - Snapshot of the class metadata used at generation time.

- `TrialPlan`
  - Ordered list of `TrialDefinition`.
  - Randomization seed and metadata.

### Runtime services

- `CueClassPresetRepository`
  - Loads/saves reusable cue/class presets.
  - Provides default autogenerated marker codes.
  - Validates duplicate codes, missing images, and disabled classes.

- `TrialPlanGenerator`
  - Generates balanced, reproducible trial orders.
  - Supports sequential, random, block-random, and constrained randomization later.

- `GrazTimelineRunner`
  - Executes the phase timeline.
  - Owns phase transitions and planned/actual timing.

- `LslMarkerOutlet`
  - Sends semantic and/or numeric markers.
  - Keeps all marker sends centralized.

- `IBciInputProvider`
  - Common interface for classifier/angle input.

- `LslBciInputProvider`
  - Reads classifier/angle values from LSL.

- `SimulatedBciInputProvider`
  - Provides keyboard/UI/manual input for offline testing.

- `FeedbackController`
  - Applies input to the prosthesis/leg.
  - Evaluates target success.

- `ExperimentLogger`
  - Writes local logs during the experiment.
  - Should not rewrite the file format at the end.

### UI separation

- Operator UI:
  - Configuration.
  - LSL status.
  - Trial plan preview.
  - Start/pause/stop.
  - Logs and diagnostics.

- Subject UI:
  - Fixation/cue/task/feedback visuals.
  - No technical clutter.
  - Suitable for fullscreen presentation.

## Initial implementation sequence

1. Document and confirm the Experiment 2 modes and required configuration fields.
2. Define the cue/class preset model and the first default Experiment 2 preset.
3. Inspect existing scenes, menus, scripts, and current GRAZ/LSL wiring.
4. Add the input-provider layer without changing visible behavior yet.
5. Add centralized LSL marker outlet.
6. Add trial plan/config classes and local log schema.
7. Implement GRAZ acquisition mode with a simple subject view.
8. Implement GRAZ feedback/test mode using simulated input first.
9. Connect feedback/test mode to real LSL classifier input.
10. Redesign the Experiment 2 menu around the four modes.
11. Iterate visually with Unity MCP screenshots and Play Mode checks.

## Open decisions

- Exact Experiment 2 class list.
- Exact marker code mapping.
- Whether LSL markers should be numeric only, string only, or both. Current preference: transmit only numeric class/phase codes through LSL and preserve rich metadata in the local log.
- Whether each mode should be a separate Unity scene or one scene with mode-specific panels/controllers.
- Whether acquisition and feedback modes should share one subject display scene.
- Target angle per class.
- Default GRAZ timing values.
- Preferred log format: CSV, JSONL, or both.
- Whether failed feedback trials should be repeated or only logged as failures.
- Where reusable presets should be saved: ScriptableObject assets, JSON files, or both.

## Recommended next decision

Define the first Experiment 2 cue/class preset:

| Enabled | Class ID | Label | Marker code | Cue image | Description | Target angle | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| true | TBD | TBD | auto/TBD | TBD | TBD | TBD | TBD |

This table should drive cue presentation, marker output, randomization, feedback targets, and logs.

## Cue/class preset behavior

The operator should be able to:

- Create a new preset.
- Duplicate an existing preset.
- Add/remove/disable classes.
- Assign a cue image to each class.
- Write a label and description for each class.
- Use autogenerated marker/class codes by default.
- Override marker/class codes manually when needed.
- Validate that all active classes have unique codes.
- Save the preset as the default for future sessions.
- Load a previous preset and modify it for a new experiment/session.

Suggested first preset:

Preset name:

```text
Simple: one leg movement at different angles
```

Classes:

| Enabled | Label | Marker code | Description | Target angle |
| --- | --- | --- | --- | --- |
| true | direita 90 | auto | Right leg at 90 degrees | 90 |
| true | direita 60 | auto | Right leg at 60 degrees | 60 |
| true | direita 45 | auto | Right leg at 45 degrees | 45 |
| true | direita 30 | auto | Right leg at 30 degrees | 30 |
| true | relaxado | auto | Relaxed/rest state | 0 |

For this preset, trial planning should treat every enabled class as a cue candidate. The number of repetitions per class determines the total number of trials, and the generated order should be randomized from the enabled class list.

## Implementation notes

### 2026-06-19

Created the first configurable cue/class foundation in the editable Unity project:

- `Assets/Scripts/Graz/ExperimentCueClass.cs`
- `Assets/Scripts/Graz/ExperimentCuePreset.cs`
- `Assets/Scripts/Graz/GrazTrialDefinition.cs`
- `Assets/Scripts/Graz/GrazTrialPlanGenerator.cs`
- `Assets/Settings/ExperimentCuePresets/Simple_RightLegAngles.asset`

The first preset contains five enabled classes:

| Label | Marker code | Target angle |
| --- | --- | --- |
| direita 90 | 1000 | 90 |
| direita 60 | 1001 | 60 |
| direita 45 | 1002 | 45 |
| direita 30 | 1003 | 30 |
| relaxado | 1004 | 0 |

Cue images are intentionally empty for now. They will be connected after the capture/print workflow and cue image library are designed.

Updated the existing photo/capture mode:

- `Assets/Scripts/Menu/PhotoMode.cs`

The photo mode now updates the visible labels with numeric readouts while active:

- left/right leg input percentage;
- left/right resulting angle;
- camera rotation;
- camera horizontal/vertical slider percentage;
- camera zoom slider percentage;
- last saved screenshot filename.

Editor preview note:

- The executable starts from `Assets/Scenes/LoadingScene.unity`.
- Testing from only `Assets/Scenes/Menu.unity` in the Editor can differ from the `.exe`, because `LoadingScene` owns the scene group loader.
- For closest parity with the `.exe`, open/play from `LoadingScene`, then follow the menu route normally.

Fixed scene group unloading:

- `Assets/Scripts/Scene/SceneGroupManager.cs`

The old scene unloading logic skipped the currently active scene. When the active scene was `Menu`, entering Experiment 2 left `Menu` loaded together with `Gameplay`, `Experimento Dois`, and `Game UI 2`. This could leave the experiment selection menu visible over the photo UI and make it look like the avatar was missing.

The loader now unloads previous group scenes and preserves only the persistent loading/bootstrap scenes.

Asset copy check:

- Compared `Assets` in `C:\Users\diego\Unity_MasterDegree_Project\UNIFESP-Project-main` against `unity_editable/UNIFESP-Project-main/Assets`.
- Missing files from `unity_editable`: 0.
- Extra files in `unity_editable`: only the new scripts/assets created during this redesign.

### 2026-06-27

Started the visual feedback overlay for photo/cue capture mode:

- `Assets/Scripts/Menu/PhotoModeFeedbackOverlay.cs`
- `Assets/Scripts/Menu/PhotoModeArcGraphic.cs`
- `Assets/Scripts/Menu/PhotoMode.cs`

Implemented runtime UI elements for:

- side-view picture-in-picture toggles for left/right;
- vertical angle progress gauges for left/right;
- target angle and tolerance controls;
- red current-angle indicator;
- green target/tolerance area;
- larger labeled marks at 0, 30, 45, 60, and 90 degrees;
- smaller intermediate marks;
- optional arc feedback overlays for left/right leg;
- a scrollable feedback-control panel.

Screenshot behavior:

- Feedback visuals remain visible in screenshots.
- Feedback controls are hidden during screenshot capture.

Initial layout rule implementation:

- If both progress gauges are enabled, side views are hidden.
- If both side views are enabled while gauges are requested, side views take priority.
- If one gauge and one side view conflict on the same side, the gauge stays on that side and the side view moves to the opposite side.

### 2026-07-24

Started the redesigned Experiment 2 operator menu:

- `Assets/Scripts/Menu/Experiment2RedesignedMenu.cs`
- `Assets/Scripts/Managers/GameUIDois.cs`

The new menu is a runtime overlay attached to `GameUIDois`. It hides the old configuration panel but keeps the existing backend fields and methods available, so the current experiment logic is not thrown away while the interface is reorganized.

Operator-facing mode structure:

| Mode | Current status | Intended route |
| --- | --- | --- |
| Captura de imagens | Button connected to the existing `PhotoMode.EntryPhotoMode()` route | Researcher-only camera/pose/screenshot workflow, no LSL required |
| Controle livre LSL | Implemented | Continuous classifier input drives either leg without any GRAZ phase; selected visual feedback remains visible until `Esc` |
| GRAZ aquisicao | UI shell prepared | Clean GRAZ protocol for EEG/EMG acquisition, with cues, markers, shuffle, and logs, but no performance feedback |
| GRAZ feedback/teste | Connected to the existing Experiment 2 start route | GRAZ-like cue/task sequence with LSL-driven leg feedback, target angle, tolerance, gravity, success criteria, and logs |

The GRAZ UI is now organized around four conceptual blocks:

- `Sessao e LSL`: stream name/type, trials, log path, load old JSON configuration.
- `Tempos do GRAZ`: baseline, fixation, task/feedback, minimum pause, maximum pause.
- `Classes, cues e markers`: future cue/class preset editor, class code/marker editor, cue image picker, and shuffled trial plan preview.
- `Criterio de sucesso e dificuldade`: gravity, target tolerance, time inside target, and future per-class/block criteria.

Important design decision:

- Gravity is treated as difficulty. Higher gravity should make the leg fall more and require more classifier input to sustain the target movement.
- GRAZ acquisition and GRAZ feedback must not share the same subject view. Acquisition should prioritize clean stimuli and markers; feedback should show classifier-driven control and performance.
- Cue classes should become editable presets, not hardcoded UI fields. Each class should eventually store label, marker code, description, target angle, cue image path, enabled/disabled state, and log metadata.

Prepared future actions in the menu:

- Validate configuration.
- Save preset.
- Generate/preview trial order.
- Add class/cue.
- Edit marker codes.
- Configure task blocks.

These buttons are intentionally present before all routes exist, so the final menu shape is already understandable and can be incremented without redesigning it again.

### 2026-08-10 - Free LSL familiarization implementation

The second Experiment 2 route is independent from the GRAZ controller. It validates only the classifier inlet, visual feedback, movement parameters, and command map. It deliberately does not require subject/session fields, a log directory, marker outlet, timings, cue images, repetitions, shuffle, or a generated trial order.

The free-control command map supports custom discrete codes while retaining the standard fallback codes (`1000`-`1004` for right/relax and `1010`-`1013` for left). A received probability scales the progressive movement speed; when the input becomes stale, both legs return toward zero at the configured fall speed.

### 2026-08-07 - GRAZ feedback V1 control contract

The first testable GRAZ feedback implementation uses two independent LSL
streams with explicit directions:

| Direction | Default name | Type | Payload |
| --- | --- | --- | --- |
| Classifier to Unity | `Simulated_Leg_Classifier` | `Classifications` | float32 `[class_code, probability]` |
| Unity to recording tools | `Unity_GRAZ_Markers` | `Markers` | numeric phase/class marker |

This V1 is deliberately experimental and may change after validation:

- the classifier emits one discrete class at a time;
- Unity drains queued LSL samples and keeps the latest valid prediction;
- class codes are mapped to the enabled cue classes configured in the preset;
- each class explicitly targets the right leg, left leg, or relaxed state;
- probability (0-1) multiplies the configured maximum movement speed;
- movement is progressive and occurs only during the `task` phase;
- stale/unknown input and the relaxed class make the commanded angles return
  toward zero using the configured gravity/fall speed;
- success requires remaining continuously inside target +/- tolerance for the
  class hold time before the fixed Task duration ends;
- optional result feedback adds `success` or `failure` after Task and before
  pause/rest.

V1 phase markers:

| Code | Phase/event |
| --- | --- |
| 1 | baseline |
| 2 | fixation |
| 3 | cue |
| 4 | task |
| 5 | pause/rest |
| 6 | success result |
| 7 | failure result |
| 1000+ | configured class/cue codes |

The menu lets the operator choose, before the run, whether the Task shows the
0-90 gauge, lateral view, and translucent target leg. Because V1 executes one
class at a time, only the current target leg is emphasized. Right and left
classes use mirrored layouts to keep the subject view readable.
