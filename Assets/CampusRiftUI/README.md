# CAMPUS RIFT UI foundation

Launch `Assets/Scenes/MainMenu.unity`. It is first in Build Settings and the Editor Play Mode start scene. The existing gameplay scene is still `Assets/Scenes/SampleScene.unity`.

## Ownership and scene flow

- `UIServices` bootstraps one `Resources/CampusRiftServices.prefab` before scene load. Only this service root and the short-lived loading overlay survive transitions. All menu/HUD panels and EventSystems belong to their scenes.
- `UIStateManager` owns gameplay input permission, cursor, pause time and listener pause. Use its API instead of writing `Time.timeScale` or cursor state from a new panel. `Changed` notifies views; `GameplayInputEnabled` gates gameplay controllers.
- `UIManager` presents the state using serialized scene references. `Settings` remembers whether it was opened from Menu or Pause. Dropdowns receive ESC before their parent. GameOver/Loading ignore ESC. Elevator's existing modal closes before ESC can open Pause.
- `GameSceneManager` uses the `SceneFlow.asset` scene paths. Async loading normalizes `progress / .9`, waits for one rendered frame and prevents duplicate load requests. `ContinueGame` is deliberately unavailable until a real save provider exists.
- `PanelTransition` and `MenuIntro` use unscaled time. Controls are disabled immediately when closing, before the fade ends.

## Existing gameplay integration

`CampusExplorer` keeps its movement, gravity, collision and camera algorithms. A small input gate returns early while blocked; pause preserves motion state. Its old cursor code is only a fallback when no UIStateManager exists. Mouse look sensitivity, zoom sensitivity and invert Y are exposed without rewriting the controller. `GameplaySettingsAdapter` applies saved settings by event.

`ElevatorInteraction` keeps its existing cabin/floor UI and logic. It hands cursor ownership to UIStateManager, suppresses interaction while paused, and prevents the same ESC from also opening Pause. E and R retain their original elevator/return bindings. No skill is bound to these keys; locked slots show `--`.

`PlayerMonsterHealth` gains a HealthChanged event and two opt-in presentation flags. The scene disables legacy health drawing and automatic death respawn. Its existing damage/protection logic and Defeated event remain in place. `PlayerDeathUIAdapter` opens GameOver; RETRY reloads the scene. The original default respawn behavior remains available for other scene/prefab consumers.

Monster AI, perception, navigation, occlusion and audio update scripts are unchanged. Existing AudioSources route to SFX or Monster groups; spatialBlend/rolloff/occlusion remain intact. AudioListener pauses with gameplay; the UI source ignores listener pause.

## Components for the next phase

- `PlayerHealthUI.Bind(PlayerMonsterHealth)` subscribes/unsubscribes health events; delayed damage fill uses a coroutine only while changing.
- `BreakthroughProgressUI.SetProgress(current, required)` reuses rune markers, shows count and a gold availability label at completion. It grants nothing.
- `SkillSlotUI.Configure(icon, key, tooltip, level)`, `SetLocked(bool)`, `SetCooldown(remaining, duration)` are presentation-only APIs. A future skill owner supplies cooldown changes. The third placeholder is Thiên Thủ Trấn Áp. Tooltip data is ready for an unlocked cursor/skill inspection screen; the combat HUD itself deliberately rejects raycasts.
- `ObjectiveUI.SetObjective(text)` and `CompleteObjective()` display mission state without mission logic.
- `MonsterWarningUI.SetWarning(bool)` is explicitly triggered, never detects or points to a monster. It is off by default.
- `GameOverUI.ShowDefeated()` can be called independently of the health adapter.
- `LearningUI` contains inactive CourseList/Lesson/Quiz/Result/Breakthrough/SkillUnlock panels. There is no learning, quiz, cultivation or unlock logic.
- Energy shows `--` and a subdued placeholder strip. It consumes no resource and introduces no stamina mechanic.

## Styling and content

Edit `CampusRiftUITheme.asset`, reusable prefabs in `Prefabs/`, and the serialized scene hierarchy. Colors and sizes are authored consistently from the theme. The builder snapshots most theme values into Graphic/TMP properties; changing the asset alone does not automatically recolor every existing scene Graphic. Button state colors and rune progress read their theme at runtime. Scene/prefab labels are serialized presentation content, so localization can replace them without changing gameplay logic. `CampusRiftFont.asset` is a dynamic TMP font using the Unity-provided Liberation Sans source (license retained under TextMesh Pro/Fonts).

Prefab set: Button_Primary, Button_Secondary, Panel_Dark, HealthBar, ProgressBar, SkillSlot, ModalPanel, LoadingScreen. HUD Graphics have raycastTarget disabled and a nonblocking CanvasGroup. Canvases use Overlay + 1920x1080 Scale With Screen Size + 0.5 match.

Main Menu uses an actual campus render and original uGUI geometry, with no heavy blur/post effects or downloaded paid art. The campus snapshot does not load the gameplay scene/monster behind the menu.

## Celestial theme (current look)

`Campus Rift/UI/Restyle - Celestial Theme` restyles the existing scenes and prefabs in place (idempotent; references and custom edits survive). Run it after any phase builder, because Phase 8 resets fonts to the legacy sans.

- Typography: `Fonts/` holds Cinzel SemiBold/ExtraBold (display, buttons, small-caps labels) and Be Vietnam Pro Medium (sentences, values, Vietnamese). Both are SIL OFL 1.1 from the official Google Fonts repository; licenses sit next to the TTFs. Cinzel falls back to Be Vietnam Pro for Vietnamese glyphs. `Campus Rift Display - Glow/Shadow.mat` are the logo underlay presets.
- Frames: `Art/Celestial/` is a 9-slice sprite set authored at 2x (Images use Pixels Per Unit Multiplier 2). The frame, gem and gold trim echo the skill-icon frames. Regenerate or tweak with `Tools/ui_make_celestial_sprites.py <out-folder>`.
- `RiftButton.Glow` fades in on hover/selection and `Gems` light up with it; `Edge` is kept for older buttons. Settings tabs/mode buttons have no gems.
- Key art: `Art/Skills/` holds the Giant Hand Seal, Void Wall and Phantom Decoy medallions. The main menu floats them with `MenuAmbience` (unscaled time, plus drifting motes), and the HUD skill slots use them as circular icons (ready = white, cooldown/empty = dimmed).
- `Art/MenuBackdrop.png` is a night render of the real campus, color-graded for the menu.
- The mobile touch buttons (`MobileControlsHUD`) still draw their procedural glyphs.

## Settings and audio

All PlayerPrefs access is centralized in SettingsManager using `CampusRift.Settings.v1`. Video options come from Screen.resolutions and QualitySettings.names (the audited project currently has one quality preset). Apply commits a draft; Cancel/ESC discard un-applied changes. Mouse controls camera orbit, camera sensitivity controls scroll zoom. Resolution/fullscreen behavior in a standalone player depends on the display/OS; Editor testing checks controls and layout, not a physical monitor mode switch.

`CampusRiftAudio.mixer` has Master, Music, SFX, Monster, UI and exposed volume parameters. Assign future music sources to Music, other effects to SFX, and new monster prefabs to Monster. `UIAudioManager` has HoverSound/ClickSound/BackSound references intentionally empty; assign licensed UI clips in the services prefab. UI interactions already have visual feedback. No monster audio is reused.

## Authoring and verification

The `Campus Rift/UI` editor commands are authoring utilities, not runtime dependencies. Scenes are saved and editable without regeneration. Phase builders are intended for initial setup; do not regenerate MainMenu over custom production edits. The Settings/Finalize steps replace their generated content.

`Run Play Acceptance Tests` runs from Play Mode and temporarily injects virtual keyboard/mouse devices. It captures five resolutions (including 16:10), tests real ESC, state priority, settings persistence, damage/death, retry/restart/round trip, movement and monster integration. Results and screenshots go to project-root `Artifacts/UI`. Validation code is wrapped in UNITY_EDITOR and excluded from builds. Use `UIValidation.SetResolution` for specific Game View sizes. The test runner restores settings and removes its devices on normal completion; stop/re-enter Play Mode to reset any failed test fixture.

Original scene/control/health/build settings copies are in project-root `Backups/UIFoundation-20260927`. Asset sources and selection decisions are documented in `UI_ASSET_AUDIT.md`.
