# Changelog

All notable changes to this project will be documented in this file.

---

## [2.0.0] - 2026-10-29

2.0 does not read 1.x dialogs or use the 1.x API.

### Added

- Explicit, non-deletable Start node; every line has a Next link (I1).
- Lines without answers continue with a click, Submit or the gamepad button through a localized "Продолжить…" prompt; the first press completes typing (C1).
- Text expressions for conditions (`Gold >= 10 && !QuestDone`) and actions (`Gold -= 10; Hired = true`), parsed once on import, with registered game functions. They replace the condition, action and composite assets (U7, I6).
- Answers can be hidden (visible-if) or locked (enabled-if); locked answers stay selectable and show a "Требуется:" hint built from the failed condition or written by hand.
- Localization hook `IDialogTextResolver` with stable keys from the dialog `Id`; editor string export through `IDialogStringSink` (U1).
- Engine-neutral sound: `[SerializeReference] IDialogSound` voices and typing sounds with a type picker, `IDialogSoundPlayer`, typing blips per author (U2).
- `IDialogInput` and the optional `InputSystemDialogInput`; the first enabled answer is selected for keyboard and gamepad (I7).
- Redesigned editor: dialog list and author palette, properties panel, answer badges, drop a link on empty space to create a linked line, validation warnings, autosave about 0.5 s after the last change with Undo/Redo (C3).
- EditMode and PlayMode tests (U6).

### Changed

- "+" creates dialogs and authors with unique names instead of overwriting (C2).
- Editor layout and groups are stored in the dialog asset by group id; the separate cache asset is gone (U5).
- Authors are read from `DialogAuthorAsset` directly; the author provider is gone, and a line without an author shows no name (I5).
- Available answers are computed once per line, and choosing an unavailable answer is ignored (I8).
- `DialogService.Play(dialog, context, token)`: the caller owns the context; `Stop()` ends `Play` normally and the token source is disposed (U4).
- Dependencies: TextMeshPro from `com.unity.ugui` 2.0.0; UniTask is a documented prerequisite (I9).
- The typewriter reveals text with `maxVisibleCharacters` on unscaled time and keeps rich-text tags (U3).
- README rewritten; install with `#v2.0.0` (U6).

### Fixed

- A dialog view without storyboard, sound player, input or continue prompt no longer throws (C4).
- Line text keeps the view's colour unless the line overrides it (I2).
- Skipping a background cross-fade no longer leaves the overlay on screen (I3).
- Autoplay cancels the view's wait for the previous line (I4).
- A view destroyed with its scene during a dialog no longer throws when the dialog ends.
- `DialogSoundPlayer` creates its audio sources in prefabs, where unassigned fields are Unity "fake nulls".

---

## [1.0.0] - 2026-04-09

### Added

- Initial release
- Graph-based dialog editor
- Dialog nodes with choices
- Conditions & actions per choice
- DialogRunner runtime system
- Voice playback support
- Storyboard (background image) support
- Animated storyboard transitions
- Autoplay / Interactive / Skip strategies
- UI system based on interfaces
- Sample scene with working dialog
- Prefab-based UI setup

### Changed

- Dialog architecture split into Core / Unity / UI layers
- Introduced StyledText abstraction

### Fixed

- GUID remapping during save
- Graph layout fallback
- Response removal edge cases