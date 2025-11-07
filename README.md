# 🎮 ARTHEMIS CONTROL

<div align="center">

![ARTHEMIS CONTROL](Assets/BANNER.png)

**Professional Gamepad Control System**

[![Version](https://img.shields.io/badge/version-6.0.3-red.svg)](https://github.com/MelAzedine/tt)
[![License](https://img.shields.io/badge/license-PRO-red.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)](https://www.microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)

*Elevate your gaming experience with professional-grade controller enhancements*

[Features](#-features) • [Installation](#-installation) • [Usage](#-usage) • [Profiles](#-game-profiles) • [FAQ](#-faq)

</div>

---

## 🌟 Features

### 🎯 Anti-Recoil System
Advanced weapon recoil compensation with:
- **Vertical & Horizontal Control**: Precise compensation for both axes
- **Timeline-Based Patterns**: Custom recoil patterns for each weapon
- **ADS Detection**: Automatic activation when aiming down sights
- **Per-Weapon Profiles**: Individual settings for 50+ weapons

### 🎪 Aim Assist
Professional-grade aim assistance with multiple modes:
- **Sinusoidal Mode**: Smooth, natural movement patterns
- **Circular Mode**: Consistent tracking around targets
- **Spiral Mode**: Advanced tracking with predictive movement
- **Customizable Parameters**: Amplitude, frequency, and intensity controls

### ⚡ Rapid Fire
Configurable rapid-fire for semi-automatic weapons:
- **Adjustable Frequency**: 1-30 Hz firing rate
- **Duty Cycle Control**: Customize shot timing
- **Per-Weapon Settings**: Different configurations for each weapon
- **Safe Defaults**: Pre-configured for popular games

### 📡 Auto Ping
Automatic enemy marking system:
- **Customizable Intervals**: Set ping frequency (500ms - 5000ms)
- **Smart Activation**: Only when needed
- **Multiple Modes**: Continuous or burst pinging

### 🎮 Controller Support
Universal controller compatibility:
- **ViGEm Integration**: Virtual Xbox 360/DualShock 4 support
- **Dead Zone Configuration**: Fine-tune stick sensitivity
- **Anti-Dead Zone**: Eliminate stick drift
- **Bezier Curves**: Advanced response curve customization
- **Button Remapping**: Full controller customization

### ⌨️ Keyboard & Mouse
Seamless KB/M integration:
- **Hybrid Support**: Use controller features with KB/M
- **Hotkey System**: Global and per-profile hotkeys
- **Mouse Sensitivity**: Precision adjustment tools

### 🎨 Visual Features
Modern, intuitive interface:
- **Interactive Controller Map**: Visual button configuration
- **Real-time Status**: Live connection and feature indicators
- **Dark Theme**: Easy on the eyes during long sessions
- **Smooth Animations**: Polished, professional feel

---

## 🎮 Game Profiles

Pre-configured profiles for popular games:

### 🏝️ Fortnite Battle Royale
Optimized for building and combat:
- Anti-Recoil: V=1.50, H=0.20
- Aim Assist: Sinusoidal, 2000/2000, 8.0 Hz
- Rapid-Fire: 12.0 Hz, 55% duty cycle
- Dead Zones: LS=0.10, RS=0.12

### 🔫 Call of Duty: Black Ops 6
Tactical shooter configuration:
- Anti-Recoil: V=1.80, H=0.30
- Aim Assist: Circular, 2500/2500, 10 Hz
- Rapid-Fire: 15 Hz, 60% duty cycle
- Optimized for fast-paced combat

### ⚔️ Call of Duty: Warzone
Battle royale specialist:
- Anti-Recoil: V=2.00, H=0.40
- Aim Assist: Spiral, 3000/3000, 12 Hz
- Rapid-Fire: 18 Hz, 65% duty cycle
- Long-range precision focus

### 🎯 Battlefield 6
Large-scale warfare settings:
- Anti-Recoil: V=1.60, H=0.25
- Aim Assist: Circular, 2200/2200, 9 Hz
- Rapid-Fire: 14 Hz, 58% duty cycle
- Vehicle and infantry balanced

---

## 📥 Installation

### Prerequisites
- **Windows 10/11** (64-bit)
- **.NET 8.0 Runtime** ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **ViGEm Bus Driver** ([Download](https://github.com/ViGEm/ViGEmBus/releases))

### Steps

1. **Download** the latest release from [Releases](https://github.com/MelAzedine/tt/releases)

2. **Install ViGEm Bus Driver**:
   ```
   Run ViGEmBus_Setup.exe as Administrator
   ```

3. **Extract** the application files to your desired location

4. **Run** `Trident.MITM.App.exe` as Administrator

5. **Enter** your license key when prompted

---

## 🚀 Usage

### Quick Start

1. **Connect Your Controller**
   - Physical controller connected via USB or Bluetooth
   - Wait for "Connected" status in bottom status bar

2. **Select Game Profile**
   - Navigate to "Profiles" tab
   - Click on your game
   - Click "Apply Profile"

3. **Activate Features**
   - Toggle features ON/OFF on the Home tab
   - Each card shows current status

4. **Configure Hotkeys** (Optional)
   - Go to Settings → Hotkeys
   - Set global toggle key (default: F8)
   - Set per-profile hotkeys

### Advanced Configuration

#### Custom Weapon Profiles
1. Go to Settings → Weapon Profiles
2. Select weapon from list
3. Adjust anti-recoil values
4. Test in training mode
5. Save profile

#### Dead Zone Tuning
1. Go to Controller tab
2. Use interactive controller map
3. Click on stick to configure
4. Adjust dead zone, anti-dead zone, max zone
5. Test stick response in real-time

#### Timeline-Based Recoil
1. Open "Draw Your Recoil" tool
2. Draw weapon recoil pattern
3. Save pattern to weapon
4. Pattern automatically applied during ADS

---

## ⚙️ System Optimization

Built-in Windows optimization tools:

### Network Optimization
- 🌐 Network latency reduction
- 🔌 Nagle algorithm disable
- 📊 High network priority
- 🎯 QoS configuration
- 🌍 DNS optimization
- 🚫 P2P update disable

### Input Lag Reduction
- 🎮 VSync disable
- 🏆 Game Mode enable
- 🖥️ Fullscreen optimization disable
- ⏱️ Timer resolution (0.5ms)
- 🖱️ Mouse acceleration disable
- ⚡ Raw input buffer disable
- 🎯 High input thread priority
- 📡 Polling rate optimization (1000 Hz)

### GPU Optimization
- 🎨 GPU scheduling disable
- 💪 High performance mode
- 🔋 Power saving disable
- 📺 Windows DVR disable
- 🎬 Hardware acceleration tuning
- 🎭 Shader optimization
- 🖼️ Tearing control

### CPU Optimization
- ⚡ Power throttling disable
- 🚫 CPU parking disable
- 💪 High performance mode
- 🔧 CPU affinity control
- 😴 C-States disable
- 🚀 Turbo boost control
- ⚡ Interrupt optimization

### System Optimization
- 🛡️ Windows Defender control
- 🔍 Superfetch/SysMain disable
- 🔎 Windows Search disable
- 📊 Telemetry disable
- 🔄 Windows Update control
- 🧹 Unnecessary services disable
- 💾 Memory optimization
- 🎨 Visual effects disable
- 📄 Pagefile configuration

---

## 🔧 Configuration Files

### trident.json
Main configuration file containing:
- Trigger mappings (Fire/ADS)
- Weapon profiles
- Haptic templates
- Timeline segments
- Feature toggles
- Hotkey settings

### Backup System
- Auto-backup on save (optional)
- Manual backup/restore
- Profile import/export

---

## 🎯 Feature Status Indicators

| Indicator | Meaning |
|-----------|---------|
| 🟢 Green Toggle | Feature Active |
| 🔴 Red Toggle | Feature Inactive |
| ✅ Connected | Device Ready |
| ❌ Disconnected | Device Not Found |
| ⚠️ Warning | Configuration Issue |

---

## 📝 Tips & Best Practices

### Performance
- ✅ Run as Administrator for full access
- ✅ Close background applications
- ✅ Use wired connection for controllers
- ✅ Apply system optimizations
- ✅ Disable Windows Game Bar

### Configuration
- ✅ Start with game profiles
- ✅ Fine-tune gradually
- ✅ Test in training mode first
- ✅ Save frequently
- ✅ Backup your profiles

### Troubleshooting
- ❌ Controller not detected? → Install ViGEm Bus Driver
- ❌ Features not working? → Run as Administrator
- ❌ High input lag? → Apply optimization preset
- ❌ Profile not loading? → Check trident.json syntax
- ❌ Hotkeys not working? → Check for key conflicts

---

## 🔐 License & Security

- **Licensed Software**: Requires valid license key
- **Secure**: Local processing, no cloud required
- **Privacy**: No data collection or telemetry
- **Updates**: Automatic license validation

---

## 🆘 Support

### Getting Help
- 📖 Check [FAQ](#-faq) section
- 💬 Discord Community (link in app)
- 📧 Email: support@arthemiscontrol.com
- 🐛 Report bugs via [GitHub Issues](https://github.com/MelAzedine/tt/issues)

---

## 📜 FAQ

**Q: Is this software safe to use?**  
A: Yes, ARTHEMIS CONTROL uses ViGEm virtual drivers and operates at the driver level. However, use responsibly and check game ToS.

**Q: Can I use this in competitive games?**  
A: This is your responsibility. Check each game's terms of service and anti-cheat policies.

**Q: Does it work with any controller?**  
A: Yes, any XInput or DirectInput controller is supported through ViGEm.

**Q: Can I create custom profiles?**  
A: Yes, fully customizable profiles for any game or weapon.

**Q: Does it work with keyboard and mouse?**  
A: Yes, hybrid support allows controller features with KB/M input.

**Q: What about input lag?**  
A: Minimal lag (<1ms) with proper system optimization applied.

**Q: Can I share my profiles?**  
A: Yes, export profiles and share with friends.

**Q: How do I update?**  
A: Check for updates in Settings → About.

---

## 🛠️ Technical Details

### Technologies Used
- **Framework**: .NET 8.0 (WPF)
- **Driver**: ViGEm Bus (Virtual Gamepad Emulation)
- **Libraries**: 
  - HidSharp (Controller I/O)
  - SharpDX.XInput (XInput Support)
  - Nefarius.ViGEm.Client (Virtual Controller)
- **UI**: XAML with custom styles and animations

### System Requirements
- **OS**: Windows 10 (1903+) or Windows 11
- **RAM**: 4 GB minimum, 8 GB recommended
- **Storage**: 100 MB free space
- **CPU**: Any x64 processor
- **USB**: 2.0+ for wired controllers
- **Bluetooth**: 4.0+ for wireless controllers

---

## 🤝 Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Open a Pull Request

---

## 📊 Version History

### v6.0.3 (Current)
- ✨ Enhanced UI with modern design
- 🎮 Improved controller detection
- 🎯 Better aim assist algorithms
- 🔧 System optimization tools
- 📱 Multi-language support

### v6.0.2
- 🐛 Bug fixes and stability improvements
- 🎨 UI refinements
- ⚡ Performance optimizations

### v6.0.1
- 🆕 Initial public release
- 🎮 Core features implemented
- 🎯 Game profiles added

---

## ⚠️ Disclaimer

This software is provided "as-is" without any warranties. Users are responsible for compliance with game terms of service and local laws. The developers assume no liability for misuse or violations.

---

## 📞 Contact

- **Website**: [arthemiscontrol.com](https://arthemiscontrol.com)
- **Discord**: [Join Community](https://discord.gg/arthemis)
- **Twitter**: [@ArthemisControl](https://twitter.com/ArthemisControl)
- **Email**: contact@arthemiscontrol.com

---

<div align="center">

**Made with ❤️ for the gaming community**

[⬆ Back to Top](#-arthemis-control)

</div>
