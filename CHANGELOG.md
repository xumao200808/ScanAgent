# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-03-17

### Added
- Phase 1 core implementation completed (100%)
- Phase 2 frontend integration completed (100%)
- Phase 3 optimization and testing completed (100%)
- Phase 4 documentation and deployment completed (100%)
- TWAIN driver integration using NTwain library
- WIA fallback service
- HTTP API endpoints:
  - GET /ping - Health check
  - GET /scanners - Enumerate scanners
  - POST /scan - Execute scan
  - GET /files/{image_id} - Get scanned image
  - DELETE /scans/{scan_id} - Cleanup temporary files
- Temporary file management (TempFileManager)
- CORS configuration for frontend integration
- Error handling with standard response format
- Scanner factory for automatic driver selection
- Unit test framework (xUnit) with 9 passing tests
- Paper size support (A4, A3, Letter, Legal)
- Frontend web interface (HTML + JavaScript)
- Agent health check and status display
- Scanner selection and parameter configuration
- Scan preview and image management
- Upload to backend integration (demo mode)
- End-to-end test script (test-e2e.bat)
- Compatibility test script (test-compatibility.bat)
- Keyboard shortcuts support (Esc, Ctrl+Enter, Ctrl+R, Ctrl+U)
- Keyboard shortcuts help dialog
- Automatic cleanup background service (IHostedService)
- File I/O buffer optimization (80KB buffer)
- Automatic old scan cleanup (24-hour retention)
- System tray application (TrayApplicationContext)
- WinForms integration for system tray support
- Dynamic tray icon generation (no external resources required)
- Tray icon context menu:
  - Open scanning interface
  - View status
  - Auto-start on boot (toggle)
  - Exit application
- Double-click tray icon to open frontend
- Balloon tip notification on startup
- Auto-start on boot via registry (HKCU\Software\Microsoft\Windows\CurrentVersion\Run)
- Status window showing:
  - Running status
  - API address
  - Total scan count
  - Uptime
- Inno Setup installer script (ScanAgent.iss)
- One-click installation to Program Files
- Automatic firewall rule configuration
- Start menu shortcuts
- Launch as normal user after installation (runasoriginaluser)
- Automatic cleanup on uninstall:
  - Firewall rule removal
  - Auto-start registry entry removal
- Project documentation:
  - User manual (USER-MANUAL.md) with installation, usage, FAQ, and troubleshooting
  - API documentation (API-DOCUMENTATION.md) with complete endpoint descriptions and examples
  - Architecture design documentation (ARCHITECTURE-DESIGN.md) with system architecture and design patterns
  - Debug guide (DEBUG-GUIDE.md) with debugging techniques and troubleshooting
  - Extension guide (EXTENSION-GUIDE.md) with extension development guidelines
  - Version release checklist (VERSION-RELEASE-CHECKLIST.md) with comprehensive release process
  - Release notes (RELEASE-NOTES.md) with version highlights and download links
- Package script (package.bat) for creating distributable ZIP packages
- Frontend build script (build-frontend.bat) for optimizing frontend resources
- One-click deployment script (deploy.bat) for automated deployment
- Phase completion reports (Phase 1, 2, 3, 4)
- Code review fix reports (multiple rounds and phases)
- End-to-end test documentation
- Compatibility test documentation

### Changed
- Changed project output type to WinExe (no console window)
- Added UseWindowsForms support
- Refactored Program.cs to use STA thread model:
  - Kestrel runs on background thread
  - WinForms message loop runs on main STA thread (TWAIN requirement)
  - Graceful shutdown on tray exit
- Removed JSON snake_case serialization (using default camelCase)
- Fixed NTwain event handler implementation
- Simplified error handling in TWAIN transfer
- Upgraded NTwain from 3.7.2 to 3.7.5
- Changed target framework from net6.0 to net6.0-windows
- Removed fetchWithRetry from non-idempotent /scan requests
- Replaced innerHTML with createElement in showError function
- Removed disk I/O from /ping health check endpoint
- Replaced Task.Run cleanup with IHostedService implementation
- Updated all documentation to version 1.0.0
- Updated README.md with latest version information

### Fixed
- Fixed ConfigureHttpJsonOptions compatibility issue with .NET 6.0
- Fixed NTwain DataTransferred event subscription
- Fixed TransferErrorEventArgs property access
- Fixed JSON serialization configuration
- Fixed event handler accumulation in TwainScannerService
- Fixed XSS vulnerabilities in frontend code
- Fixed race conditions in TempFileManager
- Fixed ScannerFactory logic issues
- Fixed ScanAsync timeout protection (2-minute timeout)
- Fixed ScannerFactory thread safety (added lock)
- Fixed ScannerFactory state logic loop
- Fixed ScannerFactory cache state residue
- Fixed NU1701 compatibility warning by upgrading NTwain to 3.7.5
- Fixed fetchWithRetry causing duplicate scans on /scan requests
- Fixed XSS vulnerability in showError innerHTML injection
- Fixed cleanup task unable to gracefully shutdown
- Fixed GetTotalScanCount causing disk I/O in health check

### Security
- Fixed XSS vulnerabilities by replacing innerHTML with textContent
- Removed server local path exposure in API responses
- Added thread safety protection for concurrent access

### Performance
- Added scanner list caching (5-second cache duration)
- Optimized concurrent scenario performance
- Avoided unnecessary TWAIN session reinitialization
- Optimized file I/O with 80KB buffer
- Removed disk I/O from health check endpoint

### Known Issues
- WIA scanner support is limited (TWAIN is primary protocol)
- Some network scanners may require additional configuration
- Correct scanner drivers must be installed

### Release Notes
- Initial release of ScanAgent v1.0.0
- Complete TWAIN scanner support
- HTTP API for third-party integration
- Web interface for easy scanning
- Comprehensive documentation
- Automated deployment scripts

### Download
- ScanAgent-1.0.0-Setup.exe (installer)
- ScanAgent-v1.0.0-win-x64.zip (portable version)

---

## [0.1.0] - 2026-03-15

### Added
- Project initialization
- Basic structure setup

---

[1.0.0]: https://github.com/flashday/ScanAgent/releases/tag/v1.0.0
[0.1.0]: https://github.com/flashday/ScanAgent/releases/tag/v0.1.0
