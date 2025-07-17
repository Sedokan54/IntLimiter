# Changelog

All notable changes to IntLimiter will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- Quota system with daily/monthly limits
- Performance optimization for large datasets
- Auto-updater with GitHub releases
- Command line interface
- Settings import/export functionality

## [1.0.0] - 2025-01-17

### Added
- Initial release of IntLimiter
- Real-time network monitoring with ETW support
- Bandwidth limiting using Windows Filtering Platform (WFP)
- Process grouping by application, service, and user
- Profile system with Gaming, Work, Streaming, and Unlimited profiles
- Statistics window with daily/monthly usage charts
- Windows toast notifications for important events
- Dark/Light theme support with runtime switching
- Connection details viewer for TCP/UDP connections
- SQLite database for persistent data storage
- System tray integration with context menu
- Process icon extraction and details
- Bandwidth limit dialog with presets
- Settings window with comprehensive configuration options
- Context menu for process management
- Real-time bandwidth charts using OxyPlot
- Modern flat UI design with responsive layout
- MVVM architecture with proper separation of concerns
- Error handling and logging throughout the application
- Admin privilege detection and requirements
- Automatic cleanup of old data and resources

### Technical Features
- WPF application built with .NET 7
- Entity Framework Core for database operations
- ETW (Event Tracing for Windows) for kernel-level monitoring
- WFP (Windows Filtering Platform) for traffic control
- Token bucket algorithm for rate limiting
- Multi-threaded architecture with proper synchronization
- Resource management with IDisposable pattern
- Configuration management with JSON serialization
- Theme system with XAML resource dictionaries
- Command pattern for UI interactions
- Observer pattern for real-time updates

### Security
- Admin privilege requirements for advanced features
- Proper WFP filter cleanup on exit
- Safe process monitoring with exception handling
- Secure configuration file handling

### Performance
- Efficient process monitoring with minimal overhead
- Optimized database queries with proper indexing
- Memory management with proper disposal patterns
- UI virtualization for large datasets
- Async/await pattern for non-blocking operations

### Compatibility
- Windows 10 and Windows 11 support
- .NET 7 runtime requirement
- x64 architecture support
- High DPI awareness

## [0.9.0] - 2025-01-16

### Added
- Beta release for testing
- Core monitoring functionality
- Basic bandwidth limiting
- SQLite database integration
- System tray support

### Known Issues
- Performance issues with large process lists
- Theme switching requires restart
- Some WFP cleanup issues on abnormal exit

## [0.8.0] - 2025-01-15

### Added
- Alpha release for internal testing
- ETW monitoring implementation
- WFP traffic control
- Basic UI framework

### Fixed
- Memory leaks in process monitoring
- UI threading issues
- Database connection problems

## [0.7.0] - 2025-01-14

### Added
- Initial development version
- Project structure setup
- Basic WPF application framework
- Model classes and services

---

## Legend

- `Added` for new features
- `Changed` for changes in existing functionality
- `Deprecated` for soon-to-be removed features
- `Removed` for now removed features
- `Fixed` for any bug fixes
- `Security` for security-related changes
- `Performance` for performance improvements
- `Technical` for technical/internal changes