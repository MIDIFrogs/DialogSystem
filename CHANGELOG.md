# Changelog

All notable changes to this project will be documented in this file.

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