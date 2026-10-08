# CAMPUS RIFT — UI asset audit

Audit date: 2026-09-27. Audited disk and the live Unity editor through its local MCP server before modifying the project.

## Available UI Assets

No suitable reusable UI asset found. No Shift, Heat, Layer Lab, imported UI prefabs, fonts or mixer were found in Assets. Installed: Unity uGUI 2.6 (including TMP), Input System 1.20, URP 17.6. Unity editor: 6000.6.2f1. The existing UI is small IMGUI health/help/elevator panels, with no Canvas or EventSystem.

Gameplay is `Assets/Scenes/SampleScene.unity`; live Player is `Campus Explorer`, using `CampusExplorer`, CharacterController and `Main Camera`. `PlayerMonsterHealth` has Damaged/Defeated UnityEvents and immediate respawn. Movement is WASD, Shift, Space; E is elevator and R is return to spawn. No grapple/swing/skill system found. Do not assign E/R to new active abilities.

## References evaluated

| Candidate / primary source | Compatibility and useful direction | Decision |
| --- | --- | --- |
| [Shift — Michsky](https://assetstore.unity.com/packages/2d/gui/shift-complete-sci-fi-ui-157943) | Store lists URP and Unity 6000.4 compatibility. Best visual reference for vertical menus, panel transitions and navigation. Exact 6000.6 integration, prefab internals and dependencies not tested. | Paid, not present. Reference only; no purchase/import. |
| [Heat — Michsky](https://assetstore.unity.com/packages/2d/gui/heat-complete-modern-ui-264857) | Store lists URP/Unity 6000.4; [publisher documentation](https://docs.michsky.com/docs/heat-ui/ui-manager/) confirms TMP customization. Clean settings/learning direction. | Paid, not present. Reference only. |
| [GUI Pro — Fantasy RPG / Layer Lab](https://assetstore.unity.com/packages/2d/gui/gui-pro-fantasy-rpg-170168) | Store lists 2022.3 URP compatibility. HUD/ability/progress reference; ornament-heavy overall style is unsuitable. Exact dependencies, TMP and Unity 6.6 unverified without package. | Paid, not present. HUD composition reference only. |
| [Sci-Fi Ui-Mix Kit / Game Fuel](https://assetstore.unity.com/packages/2d/gui/sci-fi-ui-mix-kit-313443) | Store lists 2021.3 URP compatibility. Small frames/decorations could fit; TMP, responsive states and transitions unverified. | Paid, not present. No import. |
| [Kenney UI Pack — Sci-Fi](https://kenney.nl/assets/ui-pack-sci-fi) | Official page specifies CC0 and 130 files. Engine-neutral artwork, not a tested Unity menu framework. | License permits reuse, but stock shapes do not improve the intended restrained identity enough to justify another art system. No import. |

## Asset Selected / Components Used

Native uGUI + TMP, a project-owned CampusRiftUITheme and reusable authored prefabs. TMP essentials/fonts are supplied by the installed Unity package, retaining `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`. Original lightweight uGUI geometry provides clipped panels, rune markers and rift decoration. A rendered snapshot of the existing campus is used as the menu backdrop. No external UI artwork or package was imported.

## Components Ignored

No external demo scenes, managers, cameras, input modules, audio or scripts imported. No monster clip used for UI. UIAudioManager contains optional serialized hover/click/back references pending suitable audio.

## License Notes

Commercial candidates are subject to the Standard Unity Asset Store EULA; ownership is not assumed. No transaction performed. No screenshots, sprites, scripts or sounds copied from paid packages. Kenney was evaluated at the official CC0 source only.

## Compatibility / Reason for Selection

The foundation uses installed uGUI/TMP and InputSystemUIInputModule; no new runtime dependencies. Overlay rendering is compatible with URP. Serialized theme, anchors, CanvasScaler (1920x1080, 0.5), explicit button states and unscaled transitions support reuse. Mixer groups preserve Monster spatial attenuation. Scene and control adapters preserve existing gameplay. Paid-package responsive behavior, performance, dependencies and editable prefab contents remain unverified; their visual direction alone is used as reference.

## Update 2026-09-27: Celestial theme

The first pass (default Liberation Sans, untextured procedural shapes, editor-style aerial backdrop) read as placeholder art. The restyle uses:

| Asset | Source | License | Use |
| --- | --- | --- | --- |
| Cinzel (variable, instanced to 600/800 weights with fontTools) | github.com/google/fonts `ofl/cinzel` | SIL OFL 1.1 (`Assets/CampusRiftUI/Fonts/Cinzel-OFL.txt`) | Titles, buttons, small-caps labels |
| Be Vietnam Pro Regular/Medium | github.com/google/fonts `ofl/bevietnampro` | SIL OFL 1.1 (`Fonts/BeVietnamPro-OFL.txt`) | Body text, values, Vietnamese fallback |
| Skill medallions (Giant Hand Seal, Void Wall, Phantom Decoy) | Supplied by the project owner | Project-owned | Menu key art, HUD skill icons |
| Celestial frame sprite set | Generated for this project (`Tools/ui_make_celestial_sprites.py`) | Project-owned | Buttons, panels, sliders, toggles, dividers |
| Menu backdrop | Render of the campus scene at night, color-graded | Project-owned | Main menu background |

Kenney UI Pack (CC0) was re-evaluated and again not used: its flat shapes do not match the ornate gold/violet frames of the skill art.
