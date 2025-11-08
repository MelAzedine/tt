# 📥 Installation Guide

Complete installation instructions for ARTHEMIS CONTROL

---

## Table of Contents

1. [System Requirements](#system-requirements)
2. [Prerequisites](#prerequisites)
3. [Installation Steps](#installation-steps)
4. [First Launch](#first-launch)
5. [Verification](#verification)
6. [Troubleshooting](#troubleshooting)
7. [Uninstallation](#uninstallation)

---

## System Requirements

### Minimum Requirements
- **OS**: Windows 10 (version 1903 or later)
- **Processor**: Any x64 CPU
- **RAM**: 4 GB
- **Storage**: 100 MB free space
- **Display**: 1280x720 resolution
- **Internet**: Required for license activation

### Recommended Requirements
- **OS**: Windows 11 (latest version)
- **Processor**: Intel i5 / AMD Ryzen 5 or better
- **RAM**: 8 GB or more
- **Storage**: 500 MB free space (for logs and profiles)
- **Display**: 1920x1080 or higher
- **Connection**: Wired USB for controller (Bluetooth for wireless)

### Controller Requirements
- USB 2.0+ port (wired controllers)
- Bluetooth 4.0+ (wireless controllers)
- XInput or DirectInput compatible controller

---

## Prerequisites

Before installing ARTHEMIS CONTROL, you must install:

### 1. ViGEm Bus Driver

**What is it?**  
Virtual Gamepad Emulation Framework - creates virtual Xbox/PlayStation controllers

**Installation:**

1. **Download**  
   Visit: https://github.com/ViGEm/ViGEmBus/releases/latest  
   Download: `ViGEmBus_Setup_x64.exe` (latest version)

2. **Install**  
   - Right-click downloaded file
   - Select "Run as administrator"
   - Click "Yes" on UAC prompt
   - Follow installation wizard
   - Click "Install" when prompted
   - Wait for completion

3. **Verify**  
   - Open Device Manager (Win+X → Device Manager)
   - Expand "System devices"
   - Look for "ViGEm Bus Device"
   - Should show no errors (yellow triangle)

4. **Restart**  
   Restart your computer to complete installation

### 2. .NET 8.0 Runtime

**What is it?**  
Microsoft .NET framework required to run WPF applications

**Installation:**

1. **Download**  
   Visit: https://dotnet.microsoft.com/download/dotnet/8.0  
   Select: "Desktop Runtime" for Windows x64

2. **Install**  
   - Run `windowsdesktop-runtime-8.0.x-win-x64.exe`
   - Follow installation wizard
   - Agree to license terms
   - Click "Install"
   - Wait for completion

3. **Verify**  
   Open Command Prompt and type:
   ```
   dotnet --list-runtimes
   ```
   Should show: `Microsoft.WindowsDesktop.App 8.0.x`

---

## Installation Steps

### Option 1: Installer (Recommended)

1. **Download Installer**  
   Visit: https://arthemiscontrol.com/download  
   Download: `ArthemisControl_Setup_v6.0.3.exe`

2. **Run Installer**  
   - Right-click installer
   - Select "Run as administrator"
   - Click "Yes" on UAC prompt

3. **Choose Installation Location**  
   - Default: `C:\Program Files\ArthemisControl`
   - Custom: Choose your preferred location
   - Click "Next"

4. **Select Components**  
   - ✅ ARTHEMIS CONTROL (required)
   - ✅ Desktop Shortcut (recommended)
   - ✅ Start Menu Shortcut (recommended)
   - ✅ Sample Profiles (recommended)
   - Click "Install"

5. **Wait for Installation**  
   Progress bar will show installation status

6. **Complete Installation**  
   - ✅ Create desktop shortcut
   - ✅ Launch ARTHEMIS CONTROL
   - Click "Finish"

### Option 2: Portable (ZIP)

1. **Download Portable Version**  
   Visit: https://arthemiscontrol.com/download  
   Download: `ArthemisControl_Portable_v6.0.3.zip`

2. **Extract Files**  
   - Right-click ZIP file
   - Select "Extract All..."
   - Choose destination folder
   - Click "Extract"

3. **No Installation Required**  
   Application is ready to run from extracted folder

---

## First Launch

### 1. Run Application

**Method 1: Desktop Shortcut**
- Right-click "ARTHEMIS CONTROL" icon
- Select "Run as administrator"
- Click "Yes" on UAC prompt

**Method 2: From Folder**
- Navigate to installation folder
- Right-click `Trident.MITM.App.exe`
- Select "Run as administrator"
- Click "Yes" on UAC prompt

**⚠️ Important**: Always run as Administrator for full functionality!

### 2. License Activation

**New User:**
1. Click "Enter License Key"
2. Enter your license key: `XXXX-XXXX-XXXX-XXXX`
3. Click "Validate"
4. License verified ✅

**Free Trial:**
1. Click "Start Free Trial"
2. No license key required
3. 7 days full access
4. All features unlocked

**Existing User:**
- License auto-loaded from previous installation
- Verify in License tab

### 3. First-Time Setup Wizard

**Welcome Screen:**
- Read overview
- Click "Next"

**Select Language:**
- Choose your language
- Currently: English, French
- Click "Next"

**Choose Game:**
- Select your primary game
- Pre-loads optimal settings
- Can change later
- Click "Next"

**Controller Setup:**
1. Connect your controller
2. Wait for detection
3. Green indicator = success
4. Click "Next"

**Tour (Optional):**
- Interactive feature tour
- 2-minute walkthrough
- Can skip and view later
- Click "Start" or "Skip"

### 4. Controller Connection

**Wired Controller:**
1. Plug controller into USB port
2. Wait 2-3 seconds
3. Check status: "Connected ✅"

**Wireless Controller:**
1. Enable Bluetooth on PC
2. Put controller in pairing mode:
   - **Xbox**: Hold sync button
   - **PlayStation**: Hold PS+Share
3. Pair in Windows Bluetooth settings
4. Open ARTHEMIS CONTROL
5. Check status: "Connected ✅"

**Virtual Controller:**
- Automatically created by ViGEm
- Shows in Controller tab
- Status: "Virtual X360 ✅"

---

## Verification

### 1. Check Connections

In **Controller** tab, verify:
- ✅ ViGEm Bus: Connected
- ✅ Physical Pad: Connected
- ✅ Virtual X360: Connected

All three must show "Connected" in green.

### 2. Test Features

**Anti-Recoil:**
1. Home tab → Click "No Recoil" card
2. Set values: V=1.0, H=0.0
3. Toggle ON (green)
4. Open Notepad, hold mouse button
5. Should see smooth downward movement

**Controller Test:**
1. Controller tab
2. Click interactive controller map
3. Press physical buttons
4. Should highlight on virtual map

**Profile Test:**
1. Profiles tab
2. Click any game
3. Click "Apply Profile"
4. Settings auto-loaded ✅

### 3. System Optimization

**Optional but Recommended:**
1. Optimization tab
2. Review available tweaks
3. Click "Apply Recommended Settings"
4. Restart computer
5. Enjoy improved performance!

---

## Troubleshooting

### Installation Issues

**"ViGEm Bus Driver not found"**
- Install ViGEm as Administrator
- Restart computer
- Reinstall if needed

**".NET Runtime missing"**
- Install .NET 8.0 Desktop Runtime
- Restart computer
- Try again

**"Access Denied" during installation**
- Right-click installer
- Select "Run as administrator"
- Disable antivirus temporarily
- Try again

### First Launch Issues

**Controller not detected**
- Check USB connection
- Try different USB port
- Restart application
- Check Device Manager for errors

**License validation fails**
- Check internet connection
- Verify license key format
- Contact support if issue persists

**Application crashes on startup**
- Check Event Viewer for errors
- Verify .NET is installed correctly
- Run as Administrator
- Contact support with crash log

### Common Errors

**Error: "Failed to initialize ViGEm client"**
- Solution: Reinstall ViGEm Bus Driver
- Restart computer

**Error: "No physical controller found"**
- Solution: Connect controller before starting app
- Or restart app after connecting

**Error: "Access denied to configuration file"**
- Solution: Run as Administrator
- Check folder permissions

---

## Uninstallation

### Method 1: Windows Settings

1. Open Settings (Win+I)
2. Go to Apps → Installed apps
3. Find "ARTHEMIS CONTROL"
4. Click three dots → Uninstall
5. Confirm uninstallation
6. Follow wizard prompts

### Method 2: Control Panel

1. Open Control Panel
2. Programs → Uninstall a program
3. Find "ARTHEMIS CONTROL"
4. Right-click → Uninstall
5. Follow wizard prompts

### Method 3: Uninstaller

1. Go to installation folder
2. Run `Uninstall.exe` as Administrator
3. Confirm uninstallation
4. Choose what to keep:
   - ✅ Keep profiles and settings
   - ❌ Remove everything
5. Click "Uninstall"

### Clean Removal

To completely remove all traces:

1. **Uninstall application** (methods above)

2. **Delete user data**:
   - `%AppData%\ArthemisControl`
   - Contains: profiles, settings, logs
   - Delete if you don't need backups

3. **Remove shortcuts** (if any remain):
   - Desktop
   - Start Menu
   - Quick Launch

4. **Optional: Uninstall ViGEm**:
   - Only if not used by other apps
   - Settings → Apps → ViGEm Bus Device
   - Uninstall and restart

---

## Post-Installation

### Configure Windows Firewall

ARTHEMIS CONTROL doesn't need internet except for license validation.

**Optional firewall rule:**
1. Windows Security → Firewall
2. Advanced settings
3. Outbound Rules → New Rule
4. Program → Browse to `Trident.MITM.App.exe`
5. Block connection (if desired)

### Create Backup

**Backup your configuration:**
1. Go to installation folder
2. Copy `trident.json`
3. Save to safe location
4. Restore anytime by copying back

### Configure Auto-Start (Optional)

**Run on Windows startup:**
1. Press Win+R
2. Type: `shell:startup`
3. Create shortcut to `Trident.MITM.App.exe`
4. Right-click shortcut → Properties
5. Target: Add `/minimized` flag
6. Advanced → Run as administrator

---

## Getting Help

Installation problems? We're here to help!

- 📖 [FAQ](FAQ.md) - Common questions
- 💬 [Discord](https://discord.gg/arthemis) - Community support
- 📧 [Email](mailto:support@arthemiscontrol.com) - Technical support
- 🐛 [GitHub Issues](https://github.com/MelAzedine/tt/issues) - Bug reports

---

## Next Steps

Once installed:

1. ✅ Read [Quick Start Guide](QUICKSTART.md)
2. ✅ Review [User Guide](USER_GUIDE.md)
3. ✅ Join [Discord Community](https://discord.gg/arthemis)
4. ✅ Start gaming!

---

<div align="center">

**Installation Complete! 🎉**

*Ready to dominate the game?*

[⬆ Back to Top](#-installation-guide)

</div>
