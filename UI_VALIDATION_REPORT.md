# CAMPUS RIFT — UI validation

Validated in the live Unity 6000.6.2f1 editor through local Unity MCP on 2026-09-27.

## Result

**82 acceptance checks passed, 0 failed.** The final Play Mode Console contained **0 errors and 0 warnings**. See `Artifacts/UI/UI_TEST_REPORT.json` for every assertion and `Artifacts/UI/SCENE_AUDIT.txt` for serialized-scene checks. Domain reload temporarily disconnects the existing MCP bridge and can emit its reconnect warning; no such warning remained in the final cleared Play Mode run.

## Verified behavior

- Main Menu launch, disabled Continue, Courses placeholder, scrolling Credits, Settings, and Editor `Quit requested`.
- PLAY → blocking loading overlay → original SampleScene; normalized progress; fresh health and correct state after Retry/Restart.
- Settings VIDEO/AUDIO/GAMEPLAY; resolution options from Unity; independent Monster volume through mixer; saved/reloaded preferences; sensitivity/invert applied to existing player; Cancel/reopen discards draft changes; pointer click changes a slider at the correct track position.
- Real ESC through Input System: Gameplay ↔ Pause; Settings → its origin; dropdown closes before Settings; elevator modal closes without also pausing; GameOver ignores ESC.
- Player movement and sprint; movement/jump/look blocked while paused; monster freezes during pause; monster AI movement/attack, navigation readiness and spatial audio remain active in gameplay.
- Elevator modal clears sprint telemetry and keeps camera following cabin displacement while blocking look input.
- Event-driven HP; reusable breakthrough 0/5, 3/5 and availability at 5/5; cooldown radial fill; objective completion text; explicitly triggered nondirectional warning.
- Death holds at zero HP, opens DEFEATED, does not automatically respawn, and RETRY reloads gameplay.
- Round trips and an intentionally duplicated services prefab leave one service root and one EventSystem.

## Responsive captures

Main Menu, all three Settings tabs, Pause, HUD and GameOver were checked and captured at **1920×1080, 1600×900, 1366×768, 2560×1440, 1920×1200 (16:10)**. All active controls stayed inside the screen and no TMP text was truncated. Slider fill/handle bounds were also checked after visual review identified and corrected a layout issue. There are 40 final screenshots under `Artifacts/UI`, including Courses, Credits, dropdown, Loading and the final Main Menu.

Both saved scenes have zero missing scripts, zero Legacy UI Text components in the new UI, one EventSystem, and Overlay CanvasScaler 1920×1080 / match 0.5.

## Limits and intentional placeholders

No standalone executable was built: OS fullscreen/resolution switching and process exit use Unity's APIs but were not exercised outside the Editor. The existing project provides one quality preset, so the menu correctly offers one. No new grapple/swing mechanic exists in the audited controller, so those mechanics could not be regression-tested.

Continue has no save provider. Energy, locked skills, progression and learning panels are presentation-only. Hover/click/back AudioClip references are intentionally empty. No course, quiz, skill unlock, inventory or cultivation logic was added. No commercial UI asset was purchased/imported.

Gameplay scene, core movement/health scripts and original build settings were backed up under `Backups/UIFoundation-20260927`. Editor is left stopped in MainMenu at 1920×1080; MainMenu is first in Build Settings and configured as the Editor launch scene.
