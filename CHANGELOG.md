# Changelog

All notable changes to **BuffMyBar** will be documented in this file.

This project follows the principles of **Keep a Changelog** and **Semantic Versioning**.

---

## [Unreleased]

### Added

- Native Windows 11 theme synchronization.
- Windows Theme Service.
- Buff animation framework.
- Buff Glitch animations.
- GitHub CI/CD pipeline.
- Automatic Windows Light/Dark mode detection.
- Dynamic Windows accent color support.
- CPU temperature support through an optional elevated startup task.
- Installer opt-in for elevated sensor access.
- Clean scheduled task removal during uninstall.

### Changed

- Improved widget rendering.
- Improved OBS integration.
- Refined audio visualizer.
- Cleaner widget spacing.
- Improved external monitor theme.
- Updated project documentation.
- AutoStartService now avoids Run key registration when the elevated startup task exists.
- Sensor diagnostics log path moved to `%AppData%\BuffMyBar-W26\logs`.

### Fixed

- Widget alignment issues.
- Theme synchronization bugs.
- Volume widget mute indicator.
- Volume color thresholds.
- Multi-monitor rendering improvements.

---

## [0.8.0] - 2026-06-30

### Added

- Native Windows Theme synchronization.
- WindowsThemeService.
- Buff Glitch animation engine.
- Dynamic accent color detection.
- GitHub Actions build workflow.
- GitHub Actions release workflow.

### Changed

- Removed widget borders.
- Improved visualizer integration.
- Better OBS widget visibility.
- Cleaner widget layout.
- Updated README and repository structure.

### Fixed

- Theme refresh issues.
- Volume widget rendering.
- External monitor theme consistency.
- Visualizer background rendering.

---

## [0.7.0] - 2026-06-29

### Added

- Audio Visualizer.
- Weather widget.
- Network widget.
- Battery widget.
- Media widget.
- Bluetooth widget.
- OBS widget.

### Changed

- Major UI redesign.
- Improved performance.
- Reduced memory usage.

---

## [0.6.0] - 2026-06-28

### Added

- Multi-monitor support.
- Buff accent monitor mode.
- Theme engine improvements.

### Changed

- Improved monitor detection.
- Better layout management.

---

## [0.5.0] - 2026-06-27

### Added

- Native WPF implementation.
- Initial widget framework.
- Theme engine.

---

## [0.1.0]

Initial public development.