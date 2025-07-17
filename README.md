# IntLimiter

A powerful network bandwidth monitoring and traffic control application for Windows, built with WPF and .NET 7.

![IntLimiter](https://img.shields.io/badge/Platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-7.0-purple)
![License](https://img.shields.io/badge/License-MIT-green)
![Status](https://img.shields.io/badge/Status-Active-brightgreen)

## 🚀 Features

### Core Functionality
- **Real-time Network Monitoring** - Track bandwidth usage per process with ETW (Event Tracing for Windows)
- **Bandwidth Limiting** - Control download/upload speeds using Windows Filtering Platform (WFP)
- **Process Management** - Monitor, group, and control network access for individual processes
- **Traffic Control** - Block or limit network access with kernel-level precision

### Advanced Features
- **📊 Statistics & Analytics** - Detailed usage reports with charts and graphs
- **👥 Process Grouping** - Organize processes by application, service, or custom groups
- **🎯 Profile System** - Pre-configured profiles for Gaming, Work, Streaming, and Unlimited scenarios
- **🔔 Smart Notifications** - Windows 10/11 toast notifications for important events
- **🌙 Dark/Light Theme** - Modern UI with theme switching support
- **🔍 Connection Details** - View detailed TCP/UDP connections per process
- **💾 Data Persistence** - SQLite database for historical data and settings
- **🖥️ System Tray** - Minimize to system tray with quick access

## 📸 Screenshots

### Main Window
![Main Window](screenshots/main-window.png)

### Statistics Dashboard
![Statistics](screenshots/statistics.png)

### Bandwidth Limit Dialog
![Bandwidth Limit](screenshots/bandwidth-limit.png)

### Process Grouping
![Process Grouping](screenshots/process-grouping.png)

## 🛠️ Technology Stack

- **Framework:** WPF (.NET 7)
- **Database:** SQLite with Entity Framework Core
- **Monitoring:** ETW (Event Tracing for Windows) + WMI fallback
- **Traffic Control:** Windows Filtering Platform (WFP)
- **UI Framework:** MVVM pattern with data binding
- **Charts:** OxyPlot for real-time graphs
- **Notifications:** Windows Toast API

## 📋 Requirements

- **OS:** Windows 10/11 (64-bit)
- **Runtime:** .NET 7 or later
- **Privileges:** Administrator rights (required for ETW and WFP)
- **Memory:** 100 MB RAM minimum
- **Storage:** 50 MB for application + database

## 🚀 Installation

### Option 1: Download Release
1. Go to [Releases](https://github.com/yourusername/intlimiter/releases)
2. Download the latest version
3. Run installer as Administrator
4. Follow installation wizard

### Option 2: Build from Source
```bash
# Clone repository
git clone https://github.com/yourusername/intlimiter.git
cd intlimiter

# Restore dependencies
dotnet restore

# Build application
dotnet build --configuration Release

# Run application (as Administrator)
dotnet run --project IntLimiter
```

## 🎯 Usage

### Basic Usage
1. **Run as Administrator** - Required for advanced monitoring and traffic control
2. **Monitor Processes** - View real-time bandwidth usage in the main window
3. **Set Limits** - Right-click any process to set bandwidth limits
4. **Apply Profiles** - Use pre-configured profiles for different scenarios

### Advanced Features
- **Process Grouping:** Enable grouping to organize processes by application
- **Statistics:** View detailed usage reports and export data
- **Profiles:** Create custom profiles for different usage scenarios
- **Notifications:** Configure alerts for bandwidth limits and high usage

### Keyboard Shortcuts
- `F5` - Refresh process list
- `Ctrl+T` - Toggle theme
- `Ctrl+S` - Open statistics window
- `Ctrl+P` - Open preferences

## 🔧 Configuration

### Settings Location
- **Settings:** `%APPDATA%\IntLimiter\settings.json`
- **Database:** `%APPDATA%\IntLimiter\intlimiter.db`
- **Profiles:** `%APPDATA%\IntLimiter\profiles.json`

### Profile Configuration
```json
{
  "Name": "Gaming",
  "Description": "Optimized for gaming",
  "GlobalSettings": {
    "EnableGlobalDownloadLimit": true,
    "GlobalDownloadLimit": 52428800,
    "Priority": 1
  },
  "Rules": [
    {
      "ProcessName": "steam",
      "DownloadLimit": 0,
      "UploadLimit": 0,
      "Priority": "High"
    }
  ]
}
```

## 🏗️ Architecture

### Core Components
- **NetworkMonitorService** - Process monitoring and data collection
- **BandwidthLimiterService** - Traffic control and rate limiting
- **DatabaseService** - Data persistence and statistics
- **ProfileService** - Profile management and switching
- **NotificationService** - Toast notifications and alerts

### Data Flow
```
ETW Events → NetworkMonitorService → MainViewModel → UI
     ↓                                     ↓
DatabaseService ← ProfileService ← BandwidthLimiterService
```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Setup
1. Install Visual Studio 2022 with .NET 7 SDK
2. Clone repository
3. Open `IntLimiter.sln`
4. Build and run with Administrator privileges

### Coding Standards
- Follow C# coding conventions
- Use MVVM pattern for UI code
- Add XML documentation for public APIs
- Include unit tests for new features

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🐛 Known Issues

- **Administrator Rights:** ETW monitoring and WFP traffic control require admin privileges
- **Windows Only:** Uses Windows-specific APIs (ETW, WFP)
- **Process Access:** Some system processes may not be accessible
- **Performance:** Large process lists may impact UI responsiveness

## 📊 Project Status

### Completed Features (25/30)
- ✅ Real-time network monitoring
- ✅ Bandwidth limiting and traffic control
- ✅ Process grouping and management
- ✅ Statistics and analytics
- ✅ Profile system
- ✅ Toast notifications
- ✅ Dark/Light theme support
- ✅ Connection details viewer
- ✅ SQLite database integration
- ✅ System tray integration

### Planned Features (5/30)
- ⏳ Quota system with daily/monthly limits
- ⏳ Performance optimization for large datasets
- ⏳ Auto-updater with GitHub releases
- ⏳ Command line interface
- ⏳ Settings import/export functionality

## 🙏 Acknowledgments

- Inspired by the original NetLimiter application
- Built with love for the Windows developer community
- Special thanks to Microsoft for ETW and WFP documentation
- OxyPlot team for excellent charting library

## 📞 Support

- **Issues:** [GitHub Issues](https://github.com/yourusername/intlimiter/issues)
- **Discussions:** [GitHub Discussions](https://github.com/yourusername/intlimiter/discussions)
- **Wiki:** [Project Wiki](https://github.com/yourusername/intlimiter/wiki)

---

**⭐ If you find this project useful, please give it a star!**

Made with ❤️ by [Your Name](https://github.com/yourusername)